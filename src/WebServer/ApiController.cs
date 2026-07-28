using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PathSearch.App;
using PathSearch.Parameter;
using PathSearch.Planning;

namespace PathSearch.WebServer
{
    /// <summary>맵 목록 조회, 맵/결과 이미지 서빙, Hybrid A* 경로 탐색 실행을 담당하는 REST API.</summary>
    [ApiController]
    [Route("api")]
    public sealed class ApiController : ControllerBase
    {
        private static readonly string[] SupportedMapExtensions = { ".png" };

        [HttpGet("maps")]
        public IActionResult GetMaps()
        {
            string mapDirectory = AppConfig.MapDirectory;
            if (!Directory.Exists(mapDirectory))
            {
                return Ok(Array.Empty<string>());
            }

            string[] fileNames = Directory.EnumerateFiles(mapDirectory)
                .Where(path => SupportedMapExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .Select(name => name!)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Ok(fileNames);
        }

        [HttpGet("maps/{fileName}")]
        public IActionResult GetMapImage(string fileName)
        {
            string? mapPath = ResolveExistingMapPath(fileName);
            if (mapPath is null)
            {
                return NotFound(new { error = $"맵 파일을 찾을 수 없습니다: {fileName}" });
            }

            return PhysicalFile(mapPath, "image/png");
        }

        [HttpPost("plan/{fileName}")]
        public async Task<IActionResult> Plan(string fileName, CancellationToken cancellationToken)
        {
            string? mapPath = ResolveExistingMapPath(fileName);
            if (mapPath is null)
            {
                return NotFound(new { error = $"맵 파일을 찾을 수 없습니다: {fileName}" });
            }

            PlanningPipeline.PipelineResult pipelineResult;
            try
            {
                pipelineResult = await Task.Run(
                    () => PlanningPipeline.Run(mapPath, Program.Parameters, AppConfig.ResultDirectory, cancellationToken),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 클라이언트가 "탐색 종료"로 연결을 끊은 경우: 응답은 어차피 전달되지 않지만,
                // 예외를 전역 핸들러까지 전파시키지 않고 여기서 조용히 종료한다.
                return StatusCode(499);
            }

            PlanResult plan = pipelineResult.Plan;

            return Ok(new
            {
                success = plan.Success,
                failureReason = plan.FailureReason,
                elapsedSeconds = plan.ElapsedSeconds,
                expandedNodeCount = plan.ExpandedNodeCount,
                totalCost = plan.TotalCost,
                analyticExpansionUsed = plan.AnalyticExpansionUsed,
                path = plan.Path.Select(state => new
                {
                    x = state.X,
                    y = state.Y,
                    theta = state.ThetaRad,
                    reverse = state.IsReverse,
                }),
                resultImageUrl = plan.Success ? $"/api/results/{Path.GetFileName(mapPath)}" : null,
            });
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(Program.Parameters);
        }

        [HttpPut("config")]
        public IActionResult UpdateConfig([FromBody] Parameters updated)
        {
            Program.UpdateParameters(updated);
            return Ok(Program.Parameters);
        }

        [HttpGet("results/{fileName}")]
        public IActionResult GetResultImage(string fileName)
        {
            string safeName = Path.GetFileName(fileName);
            string resultPath = Path.Combine(AppConfig.ResultDirectory, $"result_{safeName}");

            if (!System.IO.File.Exists(resultPath))
            {
                return NotFound(new { error = $"결과 이미지를 찾을 수 없습니다: {fileName}" });
            }

            return PhysicalFile(resultPath, "image/png");
        }

        // fileName은 라우트 파라미터(사용자 입력)이므로 Path.GetFileName으로 경로 조작(../ 등)을 제거한 뒤
        // MapDirectory 하위에서만 파일 존재를 확인한다.
        private static string? ResolveExistingMapPath(string fileName)
        {
            string safeName = Path.GetFileName(fileName);
            string fullPath = Path.Combine(AppConfig.MapDirectory, safeName);
            return System.IO.File.Exists(fullPath) ? fullPath : null;
        }
    }
}
