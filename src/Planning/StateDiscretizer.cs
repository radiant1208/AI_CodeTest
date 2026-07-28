using System;
using System.Runtime.CompilerServices;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning
{
    /// <summary>연속 상태(x,y,thetaRad)를 Closed 판정용 격자 인덱스(ix,iy,itheta)로 변환하고,
    /// Dictionary/HashSet의 해시 연산 오버헤드 없이 1차원 배열 기반 Direct Look-up Table로 셀별 최소 g코스트를
    /// O(1) 배열 인덱싱만으로 조회/갱신한다(맵 크기·해상도로 크기가 고정되므로 생성 시 한 번만 할당).</summary>
    public sealed class StateDiscretizer
    {
        /// <summary>LUT 배열 크기 상한(double[] 기준 약 400MB). 초과 시 GridResolution/HeadingResolutionDeg를 키우라고 안내한다.</summary>
        private const long MaxCells = 50_000_000L;

        private readonly double _gridResolution;
        private readonly double _headingResolutionRad;
        private readonly int _ixCount;
        private readonly int _iyCount;
        private readonly int _ithetaCount;
        private readonly double[] _bestCost;

        public StateDiscretizer(int mapWidth, int mapHeight, double gridResolutionPx, double headingResolutionDeg)
        {
            if (gridResolutionPx <= 0.0 || headingResolutionDeg <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridResolutionPx), "GridResolution/HeadingResolutionDeg는 0보다 커야 합니다.");
            }

            _gridResolution = gridResolutionPx;
            _headingResolutionRad = headingResolutionDeg * Math.PI / 180.0;

            // +1은 x/y/theta가 정확히 상한 경계값일 때의 floor 결과를 담기 위한 여유분이다.
            _ixCount = (int)Math.Ceiling(mapWidth / gridResolutionPx) + 1;
            _iyCount = (int)Math.Ceiling(mapHeight / gridResolutionPx) + 1;
            _ithetaCount = (int)Math.Ceiling(360.0 / headingResolutionDeg) + 1;

            long totalCells = (long)_ixCount * _iyCount * _ithetaCount;
            if (totalCells > MaxCells)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gridResolutionPx),
                    $"GridResolution/HeadingResolutionDeg가 너무 작아 Closed Set 배열 크기({totalCells:N0}셀)가 상한({MaxCells:N0}셀)을 초과합니다. 값을 키워주세요.");
            }

            _bestCost = new double[totalCells];
            Array.Fill(_bestCost, double.MaxValue);
        }

        // (x,y,thetaRad)를 배열 flat index로 변환한다. 부동소수점 경계값은 클램프해 배열 범위를 벗어나지 않게 한다.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToFlatIndex(double x, double y, double thetaRad)
        {
            int ix = Math.Clamp((int)Math.Floor(x / _gridResolution), 0, _ixCount - 1);
            int iy = Math.Clamp((int)Math.Floor(y / _gridResolution), 0, _iyCount - 1);

            double normalized = VehicleKinematics.NormalizeAngle(thetaRad);
            double positive = normalized < 0.0 ? normalized + (2.0 * Math.PI) : normalized;
            int itheta = Math.Clamp((int)Math.Floor(positive / _headingResolutionRad), 0, _ithetaCount - 1);

            return (ix * _iyCount * _ithetaCount) + (iy * _ithetaCount) + itheta;
        }

        /// <summary>g가 해당 셀의 기존 최소 g보다 작으면 갱신 후 true(계속 탐색 가치 있음), 아니면 false(Closed 처리).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUpdate(double x, double y, double thetaRad, double g)
        {
            int index = ToFlatIndex(x, y, thetaRad);
            if (_bestCost[index] <= g)
            {
                return false;
            }

            _bestCost[index] = g;
            return true;
        }

        /// <summary>(x,y,thetaRad,g)가 여전히 해당 셀의 최선 g코스트인지 여부(OpenSet Pop 시 stale 판정용).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBest(double x, double y, double thetaRad, double g)
        {
            int index = ToFlatIndex(x, y, thetaRad);
            return g <= _bestCost[index] + 1e-9;
        }
    }
}
