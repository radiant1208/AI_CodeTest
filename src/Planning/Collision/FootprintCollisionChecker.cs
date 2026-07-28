using System;
using PathSearch.Map;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Planning.Collision
{
    /// <summary>Footprint 사각형과 OccupancyGrid의 충돌 여부를 2단계로 검사한다:
    /// 1차) 차체를 감싸는 외접원(Bounding Circle) 반경으로 미리 부풀린 격자를 O(1) 조회해, 외접원조차 장애물과
    /// 겹치지 않으면 즉시 안전(no-collision) 확정하고 정밀 검사를 건너뛴다(Early-Out).
    /// 2차) 외접원이 장애물 반경 내에 있어 애매한 경우에만 사각형을 픽셀 단위로 래스터화해 정밀 검사한다(느리지만 정확).
    /// 1차 검사는 오탐(false positive, 실제로는 안 부딪히는데 부딪힌다고 판정)만 만들 수 있고 그 경우 2차로 넘기므로,
    /// 최종 판정의 정확도는 항상 2차 정밀 검사와 동일하게 유지된다.</summary>
    public sealed class FootprintCollisionChecker
    {
        private readonly OccupancyGrid _grid;
        private readonly Footprint _footprint;
        private readonly OccupancyGrid _boundingCircleGrid;

        public FootprintCollisionChecker(OccupancyGrid grid, Footprint footprint)
        {
            ArgumentNullException.ThrowIfNull(grid);
            _grid = grid;
            _footprint = footprint;

            double circumscribedRadius = Math.Sqrt(Math.Pow(footprint.Length / 2.0, 2) + Math.Pow(footprint.Width / 2.0, 2));
            _boundingCircleGrid = ObstacleInflator.Inflate(grid, circumscribedRadius);
        }

        /// <summary>pose(x,y,thetaRad)에서 footprint 사각형이 장애물 또는 맵 밖과 충돌하면 true.</summary>
        public bool IsColliding(double x, double y, double thetaRad)
        {
            int cx = (int)Math.Round(x);
            int cy = (int)Math.Round(y);
            if (!_boundingCircleGrid.IsOccupied(cx, cy))
            {
                // 외접원 반경 내에 장애물이 전혀 없음이 보장되므로, 사각형 Footprint도 절대 충돌할 수 없다.
                return false;
            }

            Span<(double X, double Y)> corners = stackalloc (double X, double Y)[4];
            _footprint.GetCorners(x, y, thetaRad, corners);

            double minX = corners[0].X, maxX = corners[0].X;
            double minY = corners[0].Y, maxY = corners[0].Y;
            for (int i = 1; i < corners.Length; i++)
            {
                if (corners[i].X < minX) minX = corners[i].X;
                if (corners[i].X > maxX) maxX = corners[i].X;
                if (corners[i].Y < minY) minY = corners[i].Y;
                if (corners[i].Y > maxY) maxY = corners[i].Y;
            }

            int startX = (int)Math.Floor(minX);
            int endX = (int)Math.Ceiling(maxX);
            int startY = (int)Math.Floor(minY);
            int endY = (int)Math.Ceiling(maxY);

            double cos = Math.Cos(thetaRad);
            double sin = Math.Sin(thetaRad);
            double halfLength = _footprint.Length / 2.0;
            double halfWidth = _footprint.Width / 2.0;

            // 회전된 사각형의 축정렬 바운딩 박스를 픽셀 단위로 순회하며, 각 픽셀 중심을 로봇 로컬 좌표계로
            // 역회전시켜 사각형 내부인지 판정한다(Footprint.GetCorners의 로컬→월드 변환의 역변환).
            for (int py = startY; py <= endY; py++)
            {
                for (int px = startX; px <= endX; px++)
                {
                    double dx = px + 0.5 - x;
                    double dy = py + 0.5 - y;
                    double localX = (dx * cos) + (dy * sin);
                    double localY = (-dx * sin) + (dy * cos);

                    if (Math.Abs(localX) > halfLength || Math.Abs(localY) > halfWidth)
                    {
                        continue;
                    }

                    if (_grid.IsOccupied(px, py))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
