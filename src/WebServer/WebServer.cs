using System;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PathSearch.App;
using PathSearch.Common;

namespace PathSearch.WebServer
{
    /// <summary>Vue 프론트엔드(wwwroot)와 REST API를 서빙하는 Kestrel 웹 서버.
    /// TaskBase를 통해 백그라운드 Task로 구동되며, 실행 중 예외가 발생하면 TaskBase의 재시도 루프에 의해
    /// 재기동을 시도한다.</summary>
    public sealed class WebServer : TaskBase
    {
        private static readonly Lazy<WebServer> _instance = new(() => new WebServer());
        public static WebServer Instance => _instance.Value;

        private WebApplication? _app;

        private WebServer() : base(nameof(WebServer), delayMilliSec: 1000)
        {
        }

        protected override async Task WorkRoutineAsync(CancellationToken ct)
        {
            _app = BuildApp();
            await _app.RunAsync(ct);
        }

        protected override void DoFinalize()
        {
            _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static WebApplication BuildApp()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://localhost:{AppConfig.WebServerPort}");

            builder.Services.AddControllers();

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
            builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

            WebApplication app = builder.Build();

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    IExceptionHandlerFeature? feature = context.Features.Get<IExceptionHandlerFeature>();
                    string message = feature?.Error.Message ?? "알 수 없는 서버 오류가 발생했습니다.";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
                });
            });

            app.UseResponseCompression();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapControllers();
            app.MapFallbackToFile("index.html");

            return app;
        }
    }
}
