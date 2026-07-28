using System;
using PathSearch.Parameter;
using PathSearch.Planning.Collision;
using PathSearch.Planning.Curves;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning
{
    /// <summary>목표까지 Reeds-Shepp/Dubins 곡선으로 한 번에 연결을 시도하는 Analytic Expansion.
    /// ReverseEnabled면 Reeds-Shepp(전후진 조합), 아니면 Dubins(전진 전용) 곡선을 선택한다.
    /// 곡선을 StepSize 이하 간격으로 샘플링해 FootprintCollisionChecker로 전부 통과해야 성공(true)으로 간주하고,
    /// 하나라도 충돌하면 즉시 중단해 실패(false)를 반환한다(모션 프리미티브 탐색으로 대체 진행).</summary>
    public sealed class AnalyticExpansion
    {
        private readonly FootprintCollisionChecker _collisionChecker;
        private readonly SearchParameters _search;
        private readonly double _turningRadius;
        private readonly bool _reverseEnabled;

        /// <summary>회전 반경(px). Goal 근접 판정(트리거 거리) 계산 등에 재사용된다.</summary>
        public double TurningRadiusPx => _turningRadius;

        public AnalyticExpansion(FootprintCollisionChecker collisionChecker, RobotParameters robot, SearchParameters search)
        {
            ArgumentNullException.ThrowIfNull(collisionChecker);
            ArgumentNullException.ThrowIfNull(robot);
            ArgumentNullException.ThrowIfNull(search);

            _collisionChecker = collisionChecker;
            _search = search;
            _turningRadius = robot.TurningRadius;
            _reverseEnabled = search.ReverseEnabled;
        }

        /// <summary>currentIndex(NodePool 노드)에서 goal pose까지 곡선 연결을 시도한다. 성공 시 같은 NodePool에
        /// currentIndex를 부모로 잇는 노드들을 이어붙이고 마지막(목표) 노드의 인덱스를 goalIndex로 반환하며 true.
        /// 곡선이 없거나 중간 샘플이 하나라도 충돌하면 false(goalIndex=-1, 풀에는 아무것도 추가하지 않음).</summary>
        public bool TryExpand(NodePool pool, int currentIndex, double goalX, double goalY, double goalThetaRad, out int goalIndex)
        {
            goalIndex = -1;
            HybridStateNode current = pool[currentIndex];

            CurvePathResult? curve = _reverseEnabled
                ? ReedsSheppPath.TryFindShortest(current.X, current.Y, current.ThetaRad, goalX, goalY, goalThetaRad, _turningRadius)
                : DubinsPath.TryFindShortest(current.X, current.Y, current.ThetaRad, goalX, goalY, goalThetaRad, _turningRadius);

            if (curve is null)
            {
                return false;
            }

            double x = current.X;
            double y = current.Y;
            double theta = current.ThetaRad;
            double g = current.G;
            bool previousReverse = current.IsReverse;
            int parentIndex = currentIndex;

            foreach (CurveSegment segment in curve.Segments)
            {
                double curvature = segment.Motion switch
                {
                    'L' => 1.0 / _turningRadius,
                    'R' => -1.0 / _turningRadius,
                    _ => 0.0,
                };

                bool isReverse = segment.SignedLengthPx < 0.0;
                double direction = isReverse ? -1.0 : 1.0;
                double remaining = Math.Abs(segment.SignedLengthPx);

                while (remaining > 1e-9)
                {
                    double step = Math.Min(_search.StepSize, remaining);
                    double arcLength = step * direction;
                    (double nx, double ny, double nTheta) = VehicleKinematics.Move(x, y, theta, curvature, arcLength);

                    if (_collisionChecker.IsColliding(nx, ny, nTheta))
                    {
                        return false;
                    }

                    double moveCost = step * (isReverse ? _search.ReversePenalty : 1.0);
                    if (isReverse != previousReverse)
                    {
                        moveCost += _search.DirectionChangePenalty;
                    }
                    g += moveCost;

                    parentIndex = pool.Add(nx, ny, nTheta, g, h: 0.0, isReverse, steeringAngleRad: 0.0, parentIndex);
                    previousReverse = isReverse;

                    x = nx;
                    y = ny;
                    theta = nTheta;
                    remaining -= step;
                }
            }

            goalIndex = parentIndex;
            return true;
        }
    }
}
