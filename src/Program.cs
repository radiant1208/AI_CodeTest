using System;
using System.IO;
using System.Threading;
using OpenCvSharp;
using PathSearch.App;
using PathSearch.IO;
using PathSearch.Parameter;
using PathSearch.Planning;
using PathSearch.Planning.Collision;
using PathSearch.Planning.Heuristics;
using PathSearch.Planning.Kinematics;
using PathSearch.Visualization;

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

            string sampleMapPath = Path.Combine(AppConfig.MapDirectory, "map1_corridor.png");
            if (File.Exists(sampleMapPath))
            {
                RunHybridAStarSelfCheck(sampleMapPath);
            }
            else
            {
                Console.WriteLine($"[Hybrid A* 자가 검증 스킵] 샘플 맵을 찾을 수 없습니다: {sampleMapPath}");
            }

            Console.WriteLine("[ctrl+c] to exit");
            _exitEvent.WaitOne();
        }

        // Step 5/6 검증용: 맵 1개에 대해 Start→Goal Hybrid A* 탐색을 실행하고 성공 여부/통계/경로를 콘솔에 출력한다.
        private static void RunHybridAStarSelfCheck(string mapPath)
        {
            Console.WriteLine();
            Console.WriteLine($"[Hybrid A* 자가 검증] 맵={Path.GetFileName(mapPath)}");

            using Mat image = MapImageLoader.Load(mapPath, Parameters.Map);
            MapParseResult parsed = MapImageParser.Parse(image);

            RobotParameters robot = Parameters.Robot;
            SearchParameters search = Parameters.Search;

            Footprint footprint = new(robot.FootprintLength, robot.FootprintWidth);
            MotionPrimitiveGenerator primitiveGenerator = new(robot, search);
            FootprintCollisionChecker collisionChecker = new(parsed.Grid, footprint);

            // Holonomic 휴리스틱은 point-robot 근사이므로, 차체를 감싸는 외접원 반경으로 장애물을 부풀린다.
            double robotRadius = Math.Sqrt(Math.Pow(robot.FootprintLength / 2.0, 2) + Math.Pow(robot.FootprintWidth / 2.0, 2));
            System.Drawing.Point goalPoint = new((int)Math.Round(parsed.Goal.X), (int)Math.Round(parsed.Goal.Y));
            HolonomicObstacleHeuristic holonomicHeuristic = new(parsed.Grid, goalPoint, robotRadius);
            NonHolonomicHeuristic nonHolonomicHeuristic = new(parsed.Goal.X, parsed.Goal.Y, parsed.Goal.HeadingRad, robot.TurningRadius, search.ReverseEnabled);
            AnalyticExpansion analyticExpansion = new(collisionChecker, robot, search);

            HybridAStarPlanner planner = new(parsed.Grid, primitiveGenerator, collisionChecker, holonomicHeuristic, nonHolonomicHeuristic, analyticExpansion, search);

            PlanResult result = planner.Search(parsed.Start.X, parsed.Start.Y, parsed.Start.HeadingRad, parsed.Goal.X, parsed.Goal.Y, parsed.Goal.HeadingRad);
            Console.WriteLine($"[탐색 결과] Success={result.Success}, 소요시간={result.ElapsedSeconds:F3}s, 확장노드수={result.ExpandedNodeCount}");

            if (result.AnalyticExpansionUsed)
            {
                Console.WriteLine("[Analytic Expansion Success] Goal connected!");
            }

            if (!result.Success)
            {
                Console.WriteLine($"[실패 사유] {result.FailureReason}");
                return;
            }

            Console.WriteLine($"[경로] 총 비용={result.TotalCost:F2}px, 경로점 개수={result.Path.Count}");
            int printCount = Math.Min(result.Path.Count, 20);
            for (int i = 0; i < printCount; i++)
            {
                HybridState state = result.Path[i];
                double headingDeg = state.ThetaRad * 180.0 / Math.PI;
                Console.WriteLine($"  [{i}] x={state.X:F1}, y={state.Y:F1}, theta={headingDeg:F1}deg, g={state.G:F2}, reverse={state.IsReverse}");
            }
            if (result.Path.Count > printCount)
            {
                Console.WriteLine($"  ... (총 {result.Path.Count}개 중 {printCount}개만 표시)");
            }

            using Mat rendered = PathOverlayRenderer.Render(image, result.Path, footprint);
            string savedResultPath = ResultImageWriter.Save(rendered, mapPath, AppConfig.ResultDirectory);
            Console.WriteLine($"[시각화 저장 완료] {savedResultPath}");
        }

        private static void CleanupResources()
        {
            _tokenSource?.Dispose();
        }
    }
}
