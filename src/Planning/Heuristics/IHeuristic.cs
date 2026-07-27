namespace PathSearch.Planning.Heuristics
{
    /// <summary>연속 좌표 (x,y,θ)에서 목표까지의 휴리스틱 거리(px)를 추정하는 공통 인터페이스.</summary>
    public interface IHeuristic
    {
        /// <summary>(x,y,headingRad)에서 목표까지의 추정 거리(px). 도달 불가능하면 double.MaxValue.</summary>
        double Estimate(double x, double y, double headingRad);
    }
}
