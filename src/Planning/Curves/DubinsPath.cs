using System;
using System.Collections.Generic;

namespace PathSearch.Planning.Curves
{
    /// <summary>Dubins 곡선: 전진 전용으로 두 pose(x,y,thetaRad)를 잇는 최단 곡선(LSL/RSR/LSR/RSL/RLR/LRL 6종
    /// 중 최솟값). NonHolonomicHeuristic의 LSL/RSR 해석해와 동일한 alpha/beta/d 파라미터화를 나머지 4종까지
    /// 확장했다. ReedsSheppPath와 달리 후진(음수 길이) 후보는 전부 배제해 순수 전진 경로만 반환한다.</summary>
    public static class DubinsPath
    {
        private readonly record struct Candidate(char[] Types, double T, double P, double Q);

        /// <summary>start pose에서 goal pose까지의 최단 Dubins 곡선. 후보가 하나도 없으면 null.</summary>
        public static CurvePathResult? TryFindShortest(
            double startX, double startY, double startThetaRad,
            double goalX, double goalY, double goalThetaRad,
            double turningRadius)
        {
            if (turningRadius <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(turningRadius), "TurningRadius는 0보다 커야 합니다.");
            }

            double dx = goalX - startX;
            double dy = goalY - startY;
            double distance = Math.Sqrt((dx * dx) + (dy * dy));
            double theta = distance < 1e-9 ? startThetaRad : Math.Atan2(dy, dx);
            double alpha = NormalizeToTwoPi(startThetaRad - theta);
            double beta = NormalizeToTwoPi(goalThetaRad - theta);
            double d = distance / turningRadius;

            List<Candidate> candidates = new();
            TryAdd(candidates, Lsl(alpha, beta, d), new[] { 'L', 'S', 'L' });
            TryAdd(candidates, Rsr(alpha, beta, d), new[] { 'R', 'S', 'R' });
            TryAdd(candidates, Lsr(alpha, beta, d), new[] { 'L', 'S', 'R' });
            TryAdd(candidates, Rsl(alpha, beta, d), new[] { 'R', 'S', 'L' });
            TryAdd(candidates, Rlr(alpha, beta, d), new[] { 'R', 'L', 'R' });
            TryAdd(candidates, Lrl(alpha, beta, d), new[] { 'L', 'R', 'L' });

            if (candidates.Count == 0)
            {
                return null;
            }

            Candidate best = candidates[0];
            double bestLength = best.T + best.P + best.Q;
            for (int i = 1; i < candidates.Count; i++)
            {
                double length = candidates[i].T + candidates[i].P + candidates[i].Q;
                if (length < bestLength)
                {
                    best = candidates[i];
                    bestLength = length;
                }
            }

            CurveSegment[] segments =
            [
                new CurveSegment(best.Types[0], best.T * turningRadius),
                new CurveSegment(best.Types[1], best.P * turningRadius),
                new CurveSegment(best.Types[2], best.Q * turningRadius),
            ];
            return new CurvePathResult(segments, bestLength * turningRadius);
        }

        // 부동소수점 경계 오차로 아주 작은 음수가 나오는 경우까지 배제해, Dubins가 절대 후진 길이를 반환하지 않도록 보장한다.
        private static void TryAdd(List<Candidate> candidates, (bool Valid, double T, double P, double Q) result, char[] types)
        {
            const double epsilon = -1e-9;
            if (result.Valid && result.T >= epsilon && result.P >= epsilon && result.Q >= epsilon)
            {
                candidates.Add(new Candidate(types, Math.Max(0.0, result.T), Math.Max(0.0, result.P), Math.Max(0.0, result.Q)));
            }
        }

        private static (bool Valid, double T, double P, double Q) Lsl(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = 2.0 + (d * d) - (2.0 * cAlphaBeta) + (2.0 * d * (sa - sb));
            if (pSquared < 0.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double tmp = Math.Atan2(cb - ca, d + sa - sb);
            double t = NormalizeToTwoPi(tmp - alpha);
            double p = Math.Sqrt(pSquared);
            double q = NormalizeToTwoPi(beta - tmp);
            return (true, t, p, q);
        }

        private static (bool Valid, double T, double P, double Q) Rsr(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = 2.0 + (d * d) - (2.0 * cAlphaBeta) + (2.0 * d * (sb - sa));
            if (pSquared < 0.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double tmp = Math.Atan2(ca - cb, d - sa + sb);
            double t = NormalizeToTwoPi(alpha - tmp);
            double p = Math.Sqrt(pSquared);
            double q = NormalizeToTwoPi(tmp - beta);
            return (true, t, p, q);
        }

        private static (bool Valid, double T, double P, double Q) Lsr(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = -2.0 + (d * d) + (2.0 * cAlphaBeta) + (2.0 * d * (sa + sb));
            if (pSquared < 0.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double p = Math.Sqrt(pSquared);
            double tmp = Math.Atan2(-ca - cb, d + sa + sb) - Math.Atan2(-2.0, p);
            double t = NormalizeToTwoPi(tmp - alpha);
            double q = NormalizeToTwoPi(tmp - NormalizeToTwoPi(beta));
            return (true, t, p, q);
        }

        private static (bool Valid, double T, double P, double Q) Rsl(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double pSquared = -2.0 + (d * d) + (2.0 * cAlphaBeta) - (2.0 * d * (sa + sb));
            if (pSquared < 0.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double p = Math.Sqrt(pSquared);
            double tmp = Math.Atan2(ca + cb, d - sa - sb) - Math.Atan2(2.0, p);
            double t = NormalizeToTwoPi(alpha - tmp);
            double q = NormalizeToTwoPi(beta - tmp);
            return (true, t, p, q);
        }

        private static (bool Valid, double T, double P, double Q) Rlr(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double tmpRlr = (6.0 - (d * d) + (2.0 * cAlphaBeta) + (2.0 * d * (sa - sb))) / 8.0;
            if (Math.Abs(tmpRlr) > 1.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double p = NormalizeToTwoPi((2.0 * Math.PI) - Math.Acos(tmpRlr));
            double t = NormalizeToTwoPi(alpha - Math.Atan2(ca - cb, d - sa + sb) + (p / 2.0));
            double q = NormalizeToTwoPi(alpha - beta - t + p);
            return (true, t, p, q);
        }

        private static (bool Valid, double T, double P, double Q) Lrl(double alpha, double beta, double d)
        {
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), ca = Math.Cos(alpha), cb = Math.Cos(beta);
            double cAlphaBeta = Math.Cos(alpha - beta);
            double tmpLrl = (6.0 - (d * d) + (2.0 * cAlphaBeta) + (2.0 * d * (sb - sa))) / 8.0;
            if (Math.Abs(tmpLrl) > 1.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double p = NormalizeToTwoPi((2.0 * Math.PI) - Math.Acos(tmpLrl));
            double t = NormalizeToTwoPi(-alpha - Math.Atan2(ca - cb, d + sa - sb) + (p / 2.0));
            double q = NormalizeToTwoPi(beta - alpha - t + p);
            return (true, t, p, q);
        }

        private static double NormalizeToTwoPi(double angle)
        {
            double twoPi = 2.0 * Math.PI;
            double result = angle % twoPi;
            return result < 0.0 ? result + twoPi : result;
        }
    }
}
