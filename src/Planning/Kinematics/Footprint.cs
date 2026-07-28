using System;
using System.Runtime.CompilerServices;

namespace PathSearch.Planning.Kinematics
{
    /// <summary>로봇 차체 형상(사각형, px 단위). pose가 주어지면 4개 꼭짓점(월드 좌표)을 계산한다.</summary>
    public readonly record struct Footprint(double Length, double Width)
    {
        /// <summary>centerX,centerY,headingRad pose 기준 4개 꼭짓점(전좌,전우,후우,후좌 순)을 corners에 채운다
        /// (길이 4 이상 필요, 할당 없음 — 충돌검사 hot path에서 stackalloc Span과 함께 쓰기 위함).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetCorners(double centerX, double centerY, double headingRad, Span<(double X, double Y)> corners)
        {
            double halfLength = Length / 2.0;
            double halfWidth = Width / 2.0;
            double cos = Math.Cos(headingRad);
            double sin = Math.Sin(headingRad);

            // 로컬 좌표계: x=전방, y=좌측. 전좌 → 전우 → 후우 → 후좌 순서로 정의.
            corners[0] = (centerX + (halfLength * cos) - (halfWidth * sin), centerY + (halfLength * sin) + (halfWidth * cos));
            corners[1] = (centerX + (halfLength * cos) - (-halfWidth * sin), centerY + (halfLength * sin) + (-halfWidth * cos));
            corners[2] = (centerX + (-halfLength * cos) - (-halfWidth * sin), centerY + (-halfLength * sin) + (-halfWidth * cos));
            corners[3] = (centerX + (-halfLength * cos) - (halfWidth * sin), centerY + (-halfLength * sin) + (halfWidth * cos));
        }
    }
}
