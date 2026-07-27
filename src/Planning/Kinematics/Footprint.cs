using System;

namespace PathSearch.Planning.Kinematics
{
    /// <summary>로봇 차체 형상(사각형, px 단위). pose가 주어지면 4개 꼭짓점(월드 좌표)을 계산한다.</summary>
    public readonly record struct Footprint(double Length, double Width)
    {
        /// <summary>centerX,centerY,headingRad pose 기준 4개 꼭짓점(전좌,전우,후우,후좌 순)을 월드 좌표로 반환한다.</summary>
        public (double X, double Y)[] GetCorners(double centerX, double centerY, double headingRad)
        {
            double halfLength = Length / 2.0;
            double halfWidth = Width / 2.0;

            // 로컬 좌표계: x=전방, y=좌측. 전좌 → 전우 → 후우 → 후좌 순서로 정의.
            Span<(double Lx, double Ly)> local =
            [
                (halfLength, halfWidth),
                (halfLength, -halfWidth),
                (-halfLength, -halfWidth),
                (-halfLength, halfWidth),
            ];

            double cos = Math.Cos(headingRad);
            double sin = Math.Sin(headingRad);

            (double X, double Y)[] corners = new (double X, double Y)[4];
            for (int i = 0; i < 4; i++)
            {
                (double lx, double ly) = local[i];
                double worldX = centerX + (lx * cos) - (ly * sin);
                double worldY = centerY + (lx * sin) + (ly * cos);
                corners[i] = (worldX, worldY);
            }

            return corners;
        }
    }
}
