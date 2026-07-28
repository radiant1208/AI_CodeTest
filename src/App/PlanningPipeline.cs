using System;
using System.Threading;
using OpenCvSharp;
using PathSearch.IO;
using PathSearch.Parameter;
using PathSearch.Planning;
using PathSearch.Planning.Collision;
using PathSearch.Planning.Heuristics;
using PathSearch.Planning.Kinematics;

namespace PathSearch.App
{
    /// <summary>맵 1개에 대해 로드→파싱→Hybrid A* 탐색→오버레이 렌더링→저장까지의 전체 흐름.
    /// CLI 자가 검증과 WebServer/ApiController 양쪽에서 동일 로직을 재사용하기 위해 분리했다.</summary>
    public static class PlanningPipeline
    {
        public sealed class PipelineResult
        {
            public required PlanResult Plan { get; init; }
            /// <summary>렌더링된 결과 이미지 저장 경로(탐색 실패 시 빈 문자열)</summary>
            public string SavedResultImagePath { get; init; } = string.Empty;
        }

        /// <summary>mapPath의 맵을 로드해 Hybrid A* 탐색을 수행하고, 성공 시 오버레이 이미지를
        /// resultDirectory에 저장한다.</summary>
        public static PipelineResult Run(string mapPath, Parameters parameters, string resultDirectory, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            using Mat image = MapImageLoader.Load(mapPath, parameters.Map);
            MapParseResult parsed = MapImageParser.Parse(image);

            RobotParameters robot = parameters.Robot;
            SearchParameters search = parameters.Search;

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
            PlanResult planResult = planner.Search(parsed.Start.X, parsed.Start.Y, parsed.Start.HeadingRad, parsed.Goal.X, parsed.Goal.Y, parsed.Goal.HeadingRad, cancellationToken);

            if (!planResult.Success)
            {
                return new PipelineResult { Plan = planResult };
            }

            using Mat rendered = PathSearch.Visualization.PathOverlayRenderer.Render(image, planResult.Path);
            string savedPath = ResultImageWriter.Save(rendered, mapPath, resultDirectory);

            return new PipelineResult { Plan = planResult, SavedResultImagePath = savedPath };
        }
    }
}
