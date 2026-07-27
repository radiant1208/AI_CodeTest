using System;
using System.Collections.Generic;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning.Curves
{
    /// <summary>Reeds-Shepp 곡선: 전진/후진을 조합해 두 pose(x,y,thetaRad)를 잇는 최단 곡선을 계산한다.
    /// LSL/LSR/LRL 3종 기본해(polar 좌표 기반, 반경=1로 정규화된 로컬 좌표계)에 timeflip(전진↔후진 반전)·
    /// reflect(좌↔우 반전) 대칭 변환을 적용해 최대 12종 후보 경로를 생성하고 그중 최단 길이를 채택한다.
    /// 전체 48-word Reeds-Shepp 중 CCCC/CCSC/CCSCC 등 희귀 word는 생략한 실용적 근사이며
    /// (NonHolonomicHeuristic의 LSL/RSR 근사와 동일한 설계 방향), AnalyticExpansion에서 정밀 충돌검사로
    /// 최종 검증되므로 근사로 인한 안전성 문제는 없다(근사로 못 찾은 경로는 모션 프리미티브 탐색으로 대체됨).</summary>
    public static class ReedsSheppPath
    {
        private readonly record struct Candidate(char[] Types, double T, double U, double V);

        /// <summary>start pose에서 goal pose까지의 최단 Reeds-Shepp 곡선. 후보가 하나도 없으면 null.</summary>
        public static CurvePathResult? TryFindShortest(
            double startX, double startY, double startThetaRad,
            double goalX, double goalY, double goalThetaRad,
            double turningRadius)
        {
            if (turningRadius <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(turningRadius), "TurningRadius는 0보다 커야 합니다.");
            }

            (double x, double y, double phi) = ToLocalFrame(startX, startY, startThetaRad, goalX, goalY, goalThetaRad, turningRadius);

            List<Candidate> candidates = new();
            CollectCandidates(candidates, x, y, phi);

            return BuildBestResult(candidates, turningRadius);
        }

        // goal을 start 기준 로컬 좌표계(start=원점, heading=0)로 변환하고 TurningRadius로 정규화(반경=1 기준)한다.
        private static (double X, double Y, double Phi) ToLocalFrame(
            double startX, double startY, double startThetaRad,
            double goalX, double goalY, double goalThetaRad,
            double turningRadius)
        {
            double dx = goalX - startX;
            double dy = goalY - startY;
            double cos = Math.Cos(startThetaRad);
            double sin = Math.Sin(startThetaRad);
            double localX = ((dx * cos) + (dy * sin)) / turningRadius;
            double localY = ((-dx * sin) + (dy * cos)) / turningRadius;
            double localPhi = VehicleKinematics.NormalizeAngle(goalThetaRad - startThetaRad);
            return (localX, localY, localPhi);
        }

        // LSL/LSR/LRL 기본해 + timeflip(부호 반전)·reflect(L/R 반전) 조합으로 최대 12개 후보를 채운다.
        private static void CollectCandidates(List<Candidate> candidates, double x, double y, double phi)
        {
            TryAdd(candidates, LeftStraightLeft(x, y, phi), new[] { 'L', 'S', 'L' });
            TryAdd(candidates, LeftStraightRight(x, y, phi), new[] { 'L', 'S', 'R' });
            TryAdd(candidates, LeftRightLeft(x, y, phi), new[] { 'L', 'R', 'L' });

            // reflect: (x,-y,-phi) 입력 + L<->R 스왑
            TryAdd(candidates, LeftStraightLeft(x, -y, -phi), new[] { 'R', 'S', 'R' });
            TryAdd(candidates, LeftStraightRight(x, -y, -phi), new[] { 'R', 'S', 'L' });
            TryAdd(candidates, LeftRightLeft(x, -y, -phi), new[] { 'R', 'L', 'R' });

            // timeflip: (-x,y,-phi) 입력 + 결과 부호 반전(후진 조합 도입)
            TryAddNegated(candidates, LeftStraightLeft(-x, y, -phi), new[] { 'L', 'S', 'L' });
            TryAddNegated(candidates, LeftStraightRight(-x, y, -phi), new[] { 'L', 'S', 'R' });
            TryAddNegated(candidates, LeftRightLeft(-x, y, -phi), new[] { 'L', 'R', 'L' });

            // timeflip + reflect: (-x,-y,phi) 입력 + L<->R 스왑 + 결과 부호 반전
            TryAddNegated(candidates, LeftStraightLeft(-x, -y, phi), new[] { 'R', 'S', 'R' });
            TryAddNegated(candidates, LeftStraightRight(-x, -y, phi), new[] { 'R', 'S', 'L' });
            TryAddNegated(candidates, LeftRightLeft(-x, -y, phi), new[] { 'R', 'L', 'R' });
        }

        private static CurvePathResult? BuildBestResult(List<Candidate> candidates, double turningRadius)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            Candidate best = candidates[0];
            double bestLength = Math.Abs(best.T) + Math.Abs(best.U) + Math.Abs(best.V);
            for (int i = 1; i < candidates.Count; i++)
            {
                double length = Math.Abs(candidates[i].T) + Math.Abs(candidates[i].U) + Math.Abs(candidates[i].V);
                if (length < bestLength)
                {
                    best = candidates[i];
                    bestLength = length;
                }
            }

            CurveSegment[] segments =
            [
                new CurveSegment(best.Types[0], best.T * turningRadius),
                new CurveSegment(best.Types[1], best.U * turningRadius),
                new CurveSegment(best.Types[2], best.V * turningRadius),
            ];
            return new CurvePathResult(segments, bestLength * turningRadius);
        }

        private static void TryAdd(List<Candidate> candidates, (bool Valid, double T, double U, double V) result, char[] types)
        {
            if (result.Valid)
            {
                candidates.Add(new Candidate(types, result.T, result.U, result.V));
            }
        }

        private static void TryAddNegated(List<Candidate> candidates, (bool Valid, double T, double U, double V) result, char[] types)
        {
            if (result.Valid)
            {
                candidates.Add(new Candidate(types, -result.T, -result.U, -result.V));
            }
        }

        // 좌회전-직진-좌회전(LSL): 반경 1인 좌회전 원(중심 (0,1)) 기준 polar 변환으로 t(회전각)·u(직진거리)·v(회전각) 계산.
        private static (bool Valid, double T, double U, double V) LeftStraightLeft(double x, double y, double phi)
        {
            (double u, double t) = Polar(x - Math.Sin(phi), y - 1.0 + Math.Cos(phi));
            if (t < 0.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double v = VehicleKinematics.NormalizeAngle(phi - t);
            return v < 0.0 ? (false, 0.0, 0.0, 0.0) : (true, t, u, v);
        }

        // 좌회전-직진-우회전(LSR): 두 원이 서로 접하는 공통 외접선 길이 기하로 t·u·v 계산.
        private static (bool Valid, double T, double U, double V) LeftStraightRight(double x, double y, double phi)
        {
            (double u1, double t1) = Polar(x + Math.Sin(phi), y - 1.0 - Math.Cos(phi));
            double u1Sq = u1 * u1;
            if (u1Sq < 4.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double u = Math.Sqrt(u1Sq - 4.0);
            double theta = Math.Atan2(2.0, u);
            double t = VehicleKinematics.NormalizeAngle(t1 + theta);
            double v = VehicleKinematics.NormalizeAngle(t - phi);
            return (t < 0.0 || v < 0.0) ? (false, 0.0, 0.0, 0.0) : (true, t, u, v);
        }

        // 좌회전-우회전-좌회전(LRL): 중간 우회전 세그먼트는 부호가 항상 <=0(진행방향이 아닌 회전 성분의 기하적 부호).
        private static (bool Valid, double T, double U, double V) LeftRightLeft(double x, double y, double phi)
        {
            (double u1, double t1) = Polar(x - Math.Sin(phi), y - 1.0 + Math.Cos(phi));
            if (u1 > 4.0)
            {
                return (false, 0.0, 0.0, 0.0);
            }

            double u = -2.0 * Math.Asin(0.25 * u1);
            double t = VehicleKinematics.NormalizeAngle(t1 + (0.5 * u) + Math.PI);
            double v = VehicleKinematics.NormalizeAngle(phi - t + u);
            return (t < 0.0 || u > 0.0) ? (false, 0.0, 0.0, 0.0) : (true, t, u, v);
        }

        private static (double R, double Theta) Polar(double x, double y) => (Math.Sqrt((x * x) + (y * y)), Math.Atan2(y, x));
    }
}
