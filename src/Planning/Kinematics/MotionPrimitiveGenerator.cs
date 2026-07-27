using System;
using System.Collections.Generic;
using PathSearch.Parameter;

namespace PathSearch.Planning.Kinematics
{
    /// <summary>모션 프리미티브 후보 1개: 다음 pose, 후진 여부, 적용된 조향각(rad).</summary>
    public readonly record struct MotionPrimitive(double X, double Y, double ThetaRad, bool IsReverse, double SteeringAngleRad);

    /// <summary>조향각 후보 × 전/후진 조합으로 현재 pose에서 이동 가능한 다음 상태 후보들을 생성한다.</summary>
    public sealed class MotionPrimitiveGenerator
    {
        private readonly SearchParameters _search;
        private readonly double _turningRadius;
        private readonly double _maxSteeringAngleRad;
        private readonly double[] _steeringAnglesRad;

        public MotionPrimitiveGenerator(RobotParameters robot, SearchParameters search)
        {
            ArgumentNullException.ThrowIfNull(robot);
            ArgumentNullException.ThrowIfNull(search);

            _search = search;
            _turningRadius = robot.TurningRadius;
            _maxSteeringAngleRad = DegToRad(robot.MaxSteeringAngleDeg);
            _steeringAnglesRad = BuildSteeringAngles(_maxSteeringAngleRad, search.SteeringAngleSamples);
        }

        /// <summary>현재 pose (x,y,thetaRad)에서 조향각 후보 × 전진(및 ReverseEnabled 시 후진) 조합의 다음 상태 후보 목록을 반환한다.</summary>
        public IReadOnlyList<MotionPrimitive> Generate(double x, double y, double thetaRad)
        {
            List<MotionPrimitive> primitives = new(_steeringAnglesRad.Length * (_search.ReverseEnabled ? 2 : 1));

            foreach (double steeringRad in _steeringAnglesRad)
            {
                double curvature = ComputeCurvature(steeringRad);

                primitives.Add(CreatePrimitive(x, y, thetaRad, curvature, steeringRad, isReverse: false));
                if (_search.ReverseEnabled)
                {
                    primitives.Add(CreatePrimitive(x, y, thetaRad, curvature, steeringRad, isReverse: true));
                }
            }

            return primitives;
        }

        private MotionPrimitive CreatePrimitive(double x, double y, double thetaRad, double curvature, double steeringRad, bool isReverse)
        {
            double arcLength = isReverse ? -_search.StepSize : _search.StepSize;
            (double nx, double ny, double nTheta) = VehicleKinematics.Move(x, y, thetaRad, curvature, arcLength);
            return new MotionPrimitive(nx, ny, nTheta, isReverse, steeringRad);
        }

        // 조향각 steeringRad를 최대 조향각 대비 선형 보간해 곡률로 변환한다.
        // steeringRad = MaxSteeringAngle일 때 curvature = 1/TurningRadius(로봇의 최소 회전 반경)가 되도록 정규화.
        private double ComputeCurvature(double steeringRad)
        {
            if (_maxSteeringAngleRad <= 0.0 || _turningRadius <= 0.0)
            {
                return 0.0;
            }

            return (steeringRad / _maxSteeringAngleRad) * (1.0 / _turningRadius);
        }

        // steeringAngleSamples개를 [-maxRad, +maxRad] 구간에 균등 분포시킨다(홀수 개일 때 정확히 0(직진)이 포함됨).
        private static double[] BuildSteeringAngles(double maxSteeringAngleRad, int steeringAngleSamples)
        {
            if (steeringAngleSamples <= 1)
            {
                return new[] { 0.0 };
            }

            double[] angles = new double[steeringAngleSamples];
            for (int i = 0; i < steeringAngleSamples; i++)
            {
                double t = -1.0 + (2.0 * i / (steeringAngleSamples - 1));
                angles[i] = t * maxSteeringAngleRad;
            }

            return angles;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    }
}
