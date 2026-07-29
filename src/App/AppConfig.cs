using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace PathSearch.App
{
    /// <summary>appsettings.json 경로 설정에 대한 정적 접근 진입점.</summary>
    public static class AppConfig
    {
        private static readonly Lazy<IConfigurationRoot> _configuration = new(Build);

        public static IConfigurationRoot Configuration => _configuration.Value;

        public static string MapDirectory => ResolveAbsolute(Configuration["MapDirectory"]);
        public static string DataDirectory => ResolveAbsolute(Configuration["DataDirectory"]);
        public static string ResultDirectory => ResolveAbsolute(Configuration["ResultDirectory"]);

        private static string ResolveAbsolute(string? configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return string.Empty;
            }
            return Path.GetFullPath(configuredPath);
        }

        /// <summary>Kestrel 웹 서버 바인딩 포트. 설정이 없으면 8888(기본값).</summary>
        public static int WebServerPort => Configuration.GetValue("WebServer:Port", 8888);

        private static IConfigurationRoot Build()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
        }

        /// <summary>필수 설정(appsettings.json 파일, MapDirectory, DataDirectory) 존재 여부를 검사한다.</summary>
        public static bool Validate(out string error)
        {
            string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(appSettingsPath))
            {
                error = $"appsettings.json 파일을 찾을 수 없습니다: {appSettingsPath}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(MapDirectory))
            {
                error = "appsettings.json에 MapDirectory 설정이 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DataDirectory))
            {
                error = "appsettings.json에 DataDirectory 설정이 없습니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
