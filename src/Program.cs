using System;
using System.IO;
using System.Threading;
using PathSearch.App;
using PathSearch.Parameter;
#if DEBUG
using System.Diagnostics;
using OpenCvSharp;
using PathSearch.IO;
using PathSearch.Map;
#endif

namespace PathSearch
{
    public static class Program
    {
        private static CancellationTokenSource? _tokenSource;
        private static CancellationToken _cancelToken;
        internal static CancellationToken CancelToken => _cancelToken;

        internal static Parameters Parameters { get; private set; } = null!;

        private static EventWaitHandle? _exitEvent;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            if (!AppConfig.Validate(out string configError))
            {
                Console.Error.WriteLine($"[설정 오류] {configError}");
                return;
            }

            _tokenSource = new CancellationTokenSource();
            _cancelToken = _tokenSource.Token;

            _exitEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                _tokenSource.Cancel();
                _exitEvent.Set();
            };

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                CleanupResources();
            };

            string parameterFilePath = Path.Combine(AppConfig.DataDirectory, "parameter.json");
            Parameters = ParameterLoader.Load(parameterFilePath);

            Console.WriteLine($"[경로 설정 로드 완료] MapDirectory={AppConfig.MapDirectory}");
            Console.WriteLine($"[경로 설정 로드 완료] ResultDirectory={AppConfig.ResultDirectory}");
            Console.WriteLine($"[파라미터 로드 완료] TurningRadius={Parameters.Robot.TurningRadius}px, StepSize={Parameters.Search.StepSize}px");

#if DEBUG
            RunObstacleInflatorDebugTest();
#endif

            Console.WriteLine("[ctrl+c] to exit");
            _exitEvent.WaitOne();
        }

        private static void CleanupResources()
        {
            _tokenSource?.Dispose();
        }

#if DEBUG
        // OccupancyGrid/ObstacleInflator 검증용 디버그 전용 테스트: maps/ 첫 번째 이미지를 부풀린 뒤
        // 원본 장애물(빨강)과 인플레이트 전용 영역(주황 반투명)을 오버레이해 test_output/에 저장한다.
        private static void RunObstacleInflatorDebugTest()
        {
            Console.WriteLine("=== [DEBUG] ObstacleInflator 검증 테스트 시작 ===");

            string[] mapFiles = Directory.GetFiles(AppConfig.MapDirectory, "*.png");
            if (mapFiles.Length == 0)
            {
                Console.WriteLine("[DEBUG 검증 실패] MapDirectory에 png 파일이 없습니다.");
                return;
            }

            string mapPath = mapFiles[0];
            Console.WriteLine($"[DEBUG] 대상 맵: {Path.GetFileName(mapPath)}");

            using Mat original = MapImageLoader.Load(mapPath, Parameters.Map);
            MapParseResult parsed = MapImageParser.Parse(original);
            OccupancyGrid grid = parsed.Grid;

            // Footprint 외접원 반지름(사각 차체를 감싸는 원) + 안전 마진(테스트 전용 상수)
            double halfLength = Parameters.Robot.FootprintLength / 2.0;
            double halfWidth = Parameters.Robot.FootprintWidth / 2.0;
            double circumscribedRadius = Math.Sqrt(halfLength * halfLength + halfWidth * halfWidth);
            const double SafetyMarginPx = 5.0;
            double radiusPx = circumscribedRadius + SafetyMarginPx;

            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            Stopwatch stopwatch = Stopwatch.StartNew();
            OccupancyGrid inflated = ObstacleInflator.Inflate(grid, radiusPx);
            stopwatch.Stop();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocBefore;

            int totalCells = grid.Width * grid.Height;
            int originalOccupied = 0;
            int inflatedOccupied = 0;
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (grid[x, y]) originalOccupied++;
                    if (inflated[x, y]) inflatedOccupied++;
                }
            }

            double originalRatio = originalOccupied * 100.0 / totalCells;
            double inflatedRatio = inflatedOccupied * 100.0 / totalCells;

            Console.WriteLine($"[DEBUG] Footprint 외접원 반지름={circumscribedRadius:F2}px + 안전마진={SafetyMarginPx:F1}px => radiusPx={radiusPx:F2}px");
            Console.WriteLine($"[DEBUG] 원본 장애물 비율: {originalRatio:F2}% ({originalOccupied}/{totalCells})");
            Console.WriteLine($"[DEBUG] 부풀려진 장애물 비율: {inflatedRatio:F2}% ({inflatedOccupied}/{totalCells})");
            Console.WriteLine($"[DEBUG] Inflate 처리 소요 시간: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"[DEBUG] Inflate 중 할당된 메모리: {allocatedBytes / 1024.0:F1} KB");

            using Mat visualization = original.Clone();
            Vec3b redColor = new(0, 0, 255);
            Vec3b orangeColor = new(0, 165, 255);
            const double OverlayAlpha = 0.45;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (grid[x, y])
                    {
                        visualization.Set(y, x, redColor);
                    }
                    else if (inflated[x, y])
                    {
                        Vec3b baseColor = visualization.At<Vec3b>(y, x);
                        visualization.Set(y, x, BlendColor(baseColor, orangeColor, OverlayAlpha));
                    }
                }
            }

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "test_output");
            Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, "inflated_map_test.png");
            Cv2.ImWrite(outputPath, visualization);

            Console.WriteLine($"[DEBUG] 시각화 결과 저장 완료: {outputPath}");
            Console.WriteLine("=== [DEBUG] ObstacleInflator 검증 테스트 종료 ===");
        }

        private static Vec3b BlendColor(Vec3b baseColor, Vec3b overlay, double alpha)
        {
            byte b = (byte)(baseColor.Item0 * (1 - alpha) + overlay.Item0 * alpha);
            byte g = (byte)(baseColor.Item1 * (1 - alpha) + overlay.Item1 * alpha);
            byte r = (byte)(baseColor.Item2 * (1 - alpha) + overlay.Item2 * alpha);
            return new Vec3b(b, g, r);
        }
#endif
    }
}
