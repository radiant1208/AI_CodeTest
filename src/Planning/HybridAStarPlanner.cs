using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using PathSearch.Map;
using PathSearch.Parameter;
using PathSearch.Planning.Collision;
using PathSearch.Planning.Heuristics;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning
{
    /// <summary>Hybrid A* 탐색 결과: 성공 여부, 역추적된 경로, 통계, 실패 사유.</summary>
    public sealed class PlanResult
    {
        public required bool Success { get; init; }
        /// <summary>시작→목표 순서로 정렬된 경로(실패 시 빈 목록)</summary>
        public IReadOnlyList<HybridState> Path { get; init; } = Array.Empty<HybridState>();
        /// <summary>OpenSet에서 Pop되어 확장된 노드 수</summary>
        public int ExpandedNodeCount { get; init; }
        public double ElapsedSeconds { get; init; }
        /// <summary>목표 노드의 누적 g코스트(px 환산, 실패 시 0)</summary>
        public double TotalCost { get; init; }
        public string FailureReason { get; init; } = string.Empty;
        /// <summary>목표 도달이 Analytic Expansion(곡선 연결)으로 이루어졌는지 여부</summary>
        public bool AnalyticExpansionUsed { get; init; }
    }

    /// <summary>Holonomic(장애물 회피) + Non-Holonomic(회전 제약) 이중 휴리스틱과 Footprint 정밀 충돌검사를 결합한 Hybrid A* 탐색기.</summary>
    public sealed class HybridAStarPlanner
    {
        // Goal과의 직선거리가 이 배수(x TurningRadius) 이내이면 간격(AnalyticExpansionInterval)과 무관하게
        // 매 확장마다 Analytic Expansion을 시도한다(곡선 연결 성공 가능성이 높은 근접 구간을 놓치지 않기 위함).
        private const double NearGoalTriggerRadiusFactor = 3.0;

        private readonly OccupancyGrid _grid;
        private readonly MotionPrimitiveGenerator _primitiveGenerator;
        private readonly FootprintCollisionChecker _collisionChecker;
        private readonly IHeuristic _holonomicHeuristic;
        private readonly IHeuristic _nonHolonomicHeuristic;
        private readonly AnalyticExpansion _analyticExpansion;
        private readonly SearchParameters _search;

        public HybridAStarPlanner(
            OccupancyGrid grid,
            MotionPrimitiveGenerator primitiveGenerator,
            FootprintCollisionChecker collisionChecker,
            IHeuristic holonomicHeuristic,
            IHeuristic nonHolonomicHeuristic,
            AnalyticExpansion analyticExpansion,
            SearchParameters search)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(primitiveGenerator);
            ArgumentNullException.ThrowIfNull(collisionChecker);
            ArgumentNullException.ThrowIfNull(holonomicHeuristic);
            ArgumentNullException.ThrowIfNull(nonHolonomicHeuristic);
            ArgumentNullException.ThrowIfNull(analyticExpansion);
            ArgumentNullException.ThrowIfNull(search);

            _grid = grid;
            _primitiveGenerator = primitiveGenerator;
            _collisionChecker = collisionChecker;
            _holonomicHeuristic = holonomicHeuristic;
            _nonHolonomicHeuristic = nonHolonomicHeuristic;
            _analyticExpansion = analyticExpansion;
            _search = search;
        }

        /// <summary>start pose에서 goal pose(허용오차 이내)까지 Hybrid A*로 탐색한다. 최대 노드 수/시간 초과 시 실패 처리.
        /// cancellationToken이 취소되면 OperationCanceledException을 던져 즉시 탐색을 중단한다(FE의 "탐색 종료" 요청 반영).</summary>
        public PlanResult Search(double startX, double startY, double startThetaRad, double goalX, double goalY, double goalThetaRad, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            StateDiscretizer discretizer = new(_grid.Width, _grid.Height, _search.GridResolution, _search.HeadingResolutionDeg);
            PriorityOpenSet openSet = new();

            double startH = CombinedHeuristic(startX, startY, startThetaRad);
            HybridState startState = new(startX, startY, startThetaRad, g: 0.0, h: startH, isReverse: false, steeringAngleRad: 0.0, parent: null);

            discretizer.TryUpdate(startX, startY, startThetaRad, 0.0);
            openSet.Push(startState);

            double goalToleranceXY = _search.GoalToleranceXY;
            double goalToleranceThetaRad = _search.GoalToleranceThetaDeg * Math.PI / 180.0;
            int expanded = 0;

            while (openSet.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (expanded >= _search.MaxSearchNodes)
                {
                    return Fail(expanded, stopwatch.Elapsed.TotalSeconds, $"최대 탐색 노드 수({_search.MaxSearchNodes})를 초과했습니다.");
                }

                if (stopwatch.Elapsed.TotalSeconds >= _search.MaxSearchSeconds)
                {
                    return Fail(expanded, stopwatch.Elapsed.TotalSeconds, $"최대 탐색 시간({_search.MaxSearchSeconds}초)을 초과했습니다.");
                }

                HybridState? current = openSet.Pop(discretizer);
                if (current is null)
                {
                    break;
                }

                expanded++;

                if (IsGoalReached(current, goalX, goalY, goalThetaRad, goalToleranceXY, goalToleranceThetaRad))
                {
                    return Succeed(current, expanded, stopwatch.Elapsed.TotalSeconds, analyticExpansionUsed: false);
                }

                bool shouldTryAnalyticExpansion = expanded % _search.AnalyticExpansionInterval == 0
                    || IsNearGoal(current, goalX, goalY, _analyticExpansion.TurningRadiusPx * NearGoalTriggerRadiusFactor);

                if (shouldTryAnalyticExpansion && _analyticExpansion.TryExpand(current, goalX, goalY, goalThetaRad, out HybridState? analyticGoal))
                {
                    return Succeed(analyticGoal!, expanded, stopwatch.Elapsed.TotalSeconds, analyticExpansionUsed: true);
                }

                foreach (MotionPrimitive primitive in _primitiveGenerator.Generate(current.X, current.Y, current.ThetaRad))
                {
                    if (_collisionChecker.IsColliding(primitive.X, primitive.Y, primitive.ThetaRad))
                    {
                        continue;
                    }

                    double moveCost = _search.StepSize;
                    if (primitive.IsReverse)
                    {
                        moveCost *= _search.ReversePenalty;
                    }
                    if (primitive.IsReverse != current.IsReverse)
                    {
                        moveCost += _search.DirectionChangePenalty;
                    }

                    double g = current.G + moveCost;
                    if (!discretizer.TryUpdate(primitive.X, primitive.Y, primitive.ThetaRad, g))
                    {
                        continue;
                    }

                    double h = CombinedHeuristic(primitive.X, primitive.Y, primitive.ThetaRad);
                    HybridState next = new(primitive.X, primitive.Y, primitive.ThetaRad, g, h, primitive.IsReverse, primitive.SteeringAngleRad, current);
                    openSet.Push(next);
                }
            }

            return Fail(expanded, stopwatch.Elapsed.TotalSeconds, "OpenSet이 비었습니다(도달 가능한 경로를 찾지 못함).");
        }

        // h(x,y,theta) = max(h_holonomic(x,y), h_nonholonomic(x,y,theta)). holonomic이 도달 불가(MaxValue)면 그대로 전파해 가지치기한다.
        private double CombinedHeuristic(double x, double y, double thetaRad)
        {
            double holonomic = _holonomicHeuristic.Estimate(x, y, thetaRad);
            if (holonomic >= double.MaxValue)
            {
                return double.MaxValue;
            }

            double nonHolonomic = _nonHolonomicHeuristic.Estimate(x, y, thetaRad);
            return Math.Max(holonomic, nonHolonomic);
        }

        private static bool IsGoalReached(HybridState state, double goalX, double goalY, double goalThetaRad, double toleranceXY, double toleranceThetaRad)
        {
            double dx = state.X - goalX;
            double dy = state.Y - goalY;
            if (Math.Sqrt((dx * dx) + (dy * dy)) > toleranceXY)
            {
                return false;
            }

            double headingDiff = Math.Abs(VehicleKinematics.NormalizeAngle(state.ThetaRad - goalThetaRad));
            return headingDiff <= toleranceThetaRad;
        }

        private static bool IsNearGoal(HybridState state, double goalX, double goalY, double triggerRadius)
        {
            double dx = state.X - goalX;
            double dy = state.Y - goalY;
            return Math.Sqrt((dx * dx) + (dy * dy)) <= triggerRadius;
        }

        private static PlanResult Succeed(HybridState goalState, int expanded, double elapsedSeconds, bool analyticExpansionUsed)
        {
            List<HybridState> path = new();
            HybridState? node = goalState;
            while (node is not null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();

            return new PlanResult
            {
                Success = true,
                Path = path,
                ExpandedNodeCount = expanded,
                ElapsedSeconds = elapsedSeconds,
                TotalCost = goalState.G,
                AnalyticExpansionUsed = analyticExpansionUsed,
            };
        }

        private static PlanResult Fail(int expanded, double elapsedSeconds, string reason)
        {
            return new PlanResult
            {
                Success = false,
                ExpandedNodeCount = expanded,
                ElapsedSeconds = elapsedSeconds,
                FailureReason = reason,
            };
        }
    }
}
