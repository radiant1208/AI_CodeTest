using System;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning.Heuristics
{
    /// <summary>회전 반경(TurningRadius) 제약만 고려하고 장애물은 무시하는 Dubins 곡선 거리 휴리스틱.
    /// LSL/RSR/LSR/RSL/RLR/LRL 6종 해석해(Planning.Curves.DubinsPath와 동일한 공식, 단 곡선의 세그먼트 배열은
    /// 만들지 않고 총 길이 스칼라만 계산해 힙 할당이 없음) 전부를 평가해 최솟값을 취한다. Dubins(1957) 정리상 이
    /// 6종 중 항상 최소 하나는 유효하므로, 유효한 후보가 하나도 없는 경우는 이론상 발생하지 않지만 부동소수점
    /// 경계 케이스에 대비해 직선거리(항상 유효한 하한)로 안전하게 폴백한다.
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

        /// <summary>(x,y,headingRad)에서 goal까지 Dubins 곡선 거리(px). 후진 허용 시 heading 반전 케이스와 비교해 최솟값 반환.</summary>
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

            double best = double.MaxValue;
            TryMin(ref best, Lsl(alpha, beta, d));
            TryMin(ref best, Rsr(alpha, beta, d));
            TryMin(ref best, Lsr(alpha, beta, d));
            TryMin(ref best, Rsl(alpha, beta, d));
            TryMin(ref best, Rlr(alpha, beta, d));
            TryMin(ref best, Lrl(alpha, beta, d));

            if (best == double.MaxValue)
            {
                // Dubins 정리상 이론적으로 도달하지 않는 방어적 분기: 직선거리는 곡률 제약과 무관하게
                // 어떤 경로 길이보다도 클 수 없으므로(삼각부등식) 항상 admissible한 하한이다.
                // (기존 구현의 "straightDistance + turningRadius*headingDiff" 폴백은 이 하한을 보장하지 못해
                // 드물게 실제 최적 비용을 과대추정할 수 있었다 — Admissible 위반 가능성이 있던 지점.)
                return straightDistance;
            }

            return best * _turningRadius;
        }

        private static void TryMin(ref double best, double? candidate)
        {
            if (candidate.HasValue && candidate.Value < best)
            {
                best = candidate.Value;
            }
        }

        // 좌회전-직진-좌회전(LSL): 두 원이 같은 방향(왼쪽)으로 접할 때의 정규화 경로 길이(반경=1 기준).
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
            double t = NormalizeToTwoPi(tmp - alpha);
            double p = Math.Sqrt(pSquared);
            double q = NormalizeToTwoPi(beta - tmp);
            return t + p + q;
        }

        // 우회전-직진-우회전(RSR): 두 원이 같은 방향(오른쪽)으로 접할 때의 정규화 경로 길이(반경=1 기준).
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
            double q = NormalizeToTwoPi(tmp - beta);
            return t + p + q;
        }

        // 좌회전-직진-우회전(LSR): 두 원이 서로 접하는 공통 외접선 길이 기하로 t·p·q 계산.
        private static double? Lsr(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = -2.0 + (d * d) + (2.0 * cAlphaBeta) + (2.0 * d * (sa + sb));
            if (pSquared < 0.0)
            {
                return null;
            }

            double p = Math.Sqrt(pSquared);
            double tmp = Math.Atan2(-ca - cb, d + sa + sb) - Math.Atan2(-2.0, p);
            double t = NormalizeToTwoPi(tmp - alpha);
            double q = NormalizeToTwoPi(tmp - NormalizeToTwoPi(beta));
            return t + p + q;
        }

        // 우회전-직진-좌회전(RSL): LSR의 대칭 형태.
        private static double? Rsl(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = -2.0 + (d * d) + (2.0 * cAlphaBeta) - (2.0 * d * (sa + sb));
            if (pSquared < 0.0)
            {
                return null;
            }

            double p = Math.Sqrt(pSquared);
            double tmp = Math.Atan2(ca + cb, d - sa - sb) - Math.Atan2(2.0, p);
            double t = NormalizeToTwoPi(alpha - tmp);
            double q = NormalizeToTwoPi(beta - tmp);
            return t + p + q;
        }

        // 우회전-좌회전-우회전(RLR): 세 원이 연속 접하는 형태, Acos 정의역(|.|<=1) 벗어나면 무효.
        private static double? Rlr(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double tmpRlr = (6.0 - (d * d) + (2.0 * cAlphaBeta) + (2.0 * d * (sa - sb))) / 8.0;
            if (Math.Abs(tmpRlr) > 1.0)
            {
                return null;
            }

            double p = NormalizeToTwoPi((2.0 * Math.PI) - Math.Acos(tmpRlr));
            double t = NormalizeToTwoPi(alpha - Math.Atan2(ca - cb, d - sa + sb) + (p / 2.0));
            double q = NormalizeToTwoPi(alpha - beta - t + p);
            return t + p + q;
        }

        // 좌회전-우회전-좌회전(LRL): RLR의 대칭 형태.
        private static double? Lrl(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double tmpLrl = (6.0 - (d * d) + (2.0 * cAlphaBeta) + (2.0 * d * (sb - sa))) / 8.0;
            if (Math.Abs(tmpLrl) > 1.0)
            {
                return null;
            }

            double p = NormalizeToTwoPi((2.0 * Math.PI) - Math.Acos(tmpLrl));
            double t = NormalizeToTwoPi(-alpha - Math.Atan2(ca - cb, d + sa - sb) + (p / 2.0));
            double q = NormalizeToTwoPi(beta - alpha - t + p);
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
