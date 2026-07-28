namespace PathSearch.Planning
{
    /// <summary>Hybrid A* 최종 경로의 노드 1개(결과 전용 불변 값 타입). 탐색 중에는 NodePool의
    /// HybridStateNode(구조체 Arena)를 사용하고, 성공 시 역추적 경로만 이 타입의 목록으로 변환해 노출한다.</summary>
    public readonly record struct HybridState(double X, double Y, double ThetaRad, bool IsReverse, double SteeringAngleRad);
}
