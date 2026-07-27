namespace PathSearch.Parameter
{
    /// <summary>data/parameter.json 최상위 파라미터 객체.</summary>
    public sealed class Parameters
    {
        public RobotParameters Robot { get; init; } = new();
        public SearchParameters Search { get; init; } = new();
    }

    /// <summary>로봇 차체 및 운동학 파라미터. 길이 단위는 px.</summary>
    public sealed class RobotParameters
    {
        /// <summary>차체 전장 (단위: px)</summary>
        public double FootprintLength { get; init; } = 25.0;
        /// <summary>차체 폭 (단위: px)</summary>
        public double FootprintWidth { get; init; } = 15.0;
        /// <summary>최소 회전 반경 (단위: px)</summary>
        public double TurningRadius { get; init; } = 30.0;
        /// <summary>최대 조향각 (단위: deg)</summary>
        public double MaxSteeringAngleDeg { get; init; } = 35.0;
    }

    /// <summary>하이브리드 A* 탐색 파라미터.</summary>
    public sealed class SearchParameters
    {
        /// <summary>모션 프리미티브 스텝 거리 (단위: px)</summary>
        public double StepSize { get; init; } = 8.0;
        /// <summary>격자 해상도 (단위: px)</summary>
        public double GridResolution { get; init; } = 4.0;
        /// <summary>heading 해상도 (단위: deg)</summary>
        public double HeadingResolutionDeg { get; init; } = 15.0;
        /// <summary>조향각 후보 개수 (단위: count)</summary>
        public int SteeringAngleSamples { get; init; } = 5;
        /// <summary>후진 허용 여부</summary>
        public bool ReverseEnabled { get; init; } = true;
        /// <summary>후진 비용 가중치 (단위: 배율)</summary>
        public double ReversePenalty { get; init; } = 2.0;
        /// <summary>전후진 전환 페널티 (단위: px)</summary>
        public double DirectionChangePenalty { get; init; } = 5.0;
        /// <summary>Analytic Expansion 시도 간격 (단위: count)</summary>
        public int AnalyticExpansionInterval { get; init; } = 10;
    }
}
