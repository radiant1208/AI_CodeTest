using System;
using System.Runtime.CompilerServices;

namespace PathSearch.Planning.Kinematics
{
    /// <summary>자전거/유니사이클 모델: 곡률(1/px)과 호 길이(arc length, px)로 다음 연속 pose (x,y,θ)를 계산.</summary>
    public static class VehicleKinematics
    {
        /// <summary>곡률이 이 값 미만이면 직선 이동으로 근사한다(0으로 나누기 방지).</summary>
        private const double CurvatureEpsilon = 1e-9;

        /// <summary>(x,y,thetaRad)에서 curvature(1/px, +좌회전/-우회전)로 arcLength(px, 후진이면 음수)만큼 이동한 다음 pose.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (double X, double Y, double ThetaRad) Move(double x, double y, double thetaRad, double curvature, double arcLength)
        {
            if (Math.Abs(curvature) < CurvatureEpsilon)
            {
                double straightX = x + arcLength * Math.Cos(thetaRad);
                double straightY = y + arcLength * Math.Sin(thetaRad);
                return (straightX, straightY, thetaRad);
            }

            double nextTheta = thetaRad + curvature * arcLength;
            double radius = 1.0 / curvature;
            double nextX = x + radius * (Math.Sin(nextTheta) - Math.Sin(thetaRad));
            double nextY = y - radius * (Math.Cos(nextTheta) - Math.Cos(thetaRad));
            return (nextX, nextY, NormalizeAngle(nextTheta));
        }

        /// <summary>각도를 [-π, π) 범위로 정규화한다.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngle(double angleRad)
        {
            double twoPi = 2.0 * Math.PI;
            return angleRad - twoPi * Math.Floor((angleRad + Math.PI) / twoPi);
        }
    }
}
