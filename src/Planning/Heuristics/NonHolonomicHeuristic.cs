using System;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning.Heuristics
{
    /// <summary>회전 반경(TurningRadius) 제약만 고려하고 장애물은 무시하는 Dubins 곡선 거리 근사 휴리스틱.
    /// 전체 Reeds-Shepp/Dubins word 대신 동일 방향 회전 조합(LSL/RSR)의 해석해만 계산하고,
    /// 두 원이 반대로 접하는 S자 경로(해석해 미해결 케이스)는 유클리드 거리 + 회전 보정항으로 근사한다.
    /// ReverseEnabled면 heading을 π 반전한 값으로도 계산해 더 짧은 쪽을 채택한다(후진 근사).</summary>
    public sealed class NonHolonomicHeuristic : IHeuristic
    {
        private readonly double _goalX;
        private readonly double _goalY;
        private readonly double _goalThetaRad;
        private readonly double _turningRadius;
        private readonly bool _reverseEnabled;

        public NonHolonomicHeuristic(double goalX, double goalY, double goalThetaRad, double turningRadius, bool reverseEnabled)
        {
            if (turningRadius <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(turningRadius), "TurningRadius는 0보다 커야 합니다.");
            }

            _goalX = goalX;
            _goalY = goalY;
            _goalThetaRad = goalThetaRad;
            _turningRadius = turningRadius;
            _reverseEnabled = reverseEnabled;
        }

        /// <summary>(x,y,headingRad)에서 goal까지 Dubins 근사 곡선 거리(px). 후진 허용 시 heading 반전 케이스와 비교해 최솟값 반환.</summary>
        public double Estimate(double x, double y, double headingRad)
        {
            double forward = CurveDistance(x, y, headingRad);
            if (!_reverseEnabled)
            {
                return forward;
            }

            double reversedHeading = VehicleKinematics.NormalizeAngle(headingRad + Math.PI);
            double reversed = CurveDistance(x, y, reversedHeading);
            return Math.Min(forward, reversed);
        }

        private double CurveDistance(double x, double y, double headingRad)
        {
            double dx = _goalX - x;
            double dy = _goalY - y;
            double straightDistance = Math.Sqrt((dx * dx) + (dy * dy));

            if (straightDistance < 1e-9)
            {
                double headingOnlyDiff = Math.Abs(VehicleKinematics.NormalizeAngle(_goalThetaRad - headingRad));
                return headingOnlyDiff * _turningRadius;
            }

            double theta = Math.Atan2(dy, dx);
            double alpha = NormalizeToTwoPi(headingRad - theta);
            double beta = NormalizeToTwoPi(_goalThetaRad - theta);
            double d = straightDistance / _turningRadius;

            double? lsl = Lsl(alpha, beta, d);
            double? rsr = Rsr(alpha, beta, d);

            double best = double.MaxValue;
            if (lsl.HasValue)
            {
                best = Math.Min(best, lsl.Value);
            }
            if (rsr.HasValue)
            {
                best = Math.Min(best, rsr.Value);
            }

            if (best == double.MaxValue)
            {
                double headingDiff = Math.Abs(VehicleKinematics.NormalizeAngle(_goalThetaRad - headingRad));
                return straightDistance + (_turningRadius * headingDiff);
            }

            return best * _turningRadius;
        }

        // 좌회전-직진-좌회전(LSL) 해석해: 두 원이 같은 방향(왼쪽)으로 접할 때의 정규화 경로 길이(반경=1 기준).
        private static double? Lsl(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = 2.0 + (d * d) - (2.0 * cAlphaBeta) + (2.0 * d * (sa - sb));
            if (pSquared < 0.0)
            {
                return null;
            }

            double tmp = Math.Atan2(cb - ca, d + sa - sb);
            double t = NormalizeToTwoPi(-alpha + tmp);
            double p = Math.Sqrt(pSquared);
            double q = NormalizeToTwoPi(beta - tmp);
            return t + p + q;
        }

        // 우회전-직진-우회전(RSR) 해석해: 두 원이 같은 방향(오른쪽)으로 접할 때의 정규화 경로 길이(반경=1 기준).
        private static double? Rsr(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = 2.0 + (d * d) - (2.0 * cAlphaBeta) + (2.0 * d * (sb - sa));
            if (pSquared < 0.0)
            {
                return null;
            }

            double tmp = Math.Atan2(ca - cb, d - sa + sb);
            double t = NormalizeToTwoPi(alpha - tmp);
            double p = Math.Sqrt(pSquared);
            double q = NormalizeToTwoPi(-beta + tmp);
            return t + p + q;
        }

        private static double NormalizeToTwoPi(double angle)
        {
            double twoPi = 2.0 * Math.PI;
            double result = angle % twoPi;
            return result < 0.0 ? result + twoPi : result;
        }
    }
}
