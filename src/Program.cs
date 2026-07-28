using System;
using System.IO;
using System.Threading;
using PathSearch.App;
using PathSearch.Parameter;
using PathSearch.WebServer;

namespace PathSearch
{
    public static class Program
    {
        private static CancellationTokenSource? _tokenSource;
        private static CancellationToken _cancelToken;
        internal static CancellationToken CancelToken => _cancelToken;

        internal static Parameters Parameters { get; private set; } = null!;

        private static EventWaitHandle? _exitEvent;

        /// <summary>FE에서 수정한 파라미터를 즉시 반영(이후 Plan 호출부터 적용)하고 data/parameter.json에 영속화한다.</summary>
        internal static void UpdateParameters(Parameters updated)
        {
            Parameters = updated;
            string parameterFilePath = Path.Combine(AppConfig.DataDirectory, "parameter.json");
            ParameterLoader.Save(parameterFilePath, updated);
        }

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

            WebServer.WebServer.Instance.RunAsync(_cancelToken);
            Console.WriteLine($"[WebServer 시작] http://localhost:{AppConfig.WebServerPort}");

            Console.WriteLine("[ctrl+c] to exit");
            _exitEvent.WaitOne();
        }

        private static void CleanupResources()
        {
            _tokenSource?.Dispose();
        }
    }
}
