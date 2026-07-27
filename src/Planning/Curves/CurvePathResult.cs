using System.Collections.Generic;

namespace PathSearch.Planning.Curves
{
    /// <summary>곡선 경로의 세그먼트 1개. Motion: 'L'(좌회전)/'R'(우회전)/'S'(직진), SignedLengthPx: 부호 있는 길이(+전진/-후진, px)</summary>
    public readonly record struct CurveSegment(char Motion, double SignedLengthPx);

    /// <summary>Analytic Expansion용 곡선 계산 결과: 세그먼트 목록과 총 길이(px, 절대값 합).</summary>
    public sealed class CurvePathResult
    {
        public IReadOnlyList<CurveSegment> Segments { get; }
        public double TotalLengthPx { get; }

        public CurvePathResult(IReadOnlyList<CurveSegment> segments, double totalLengthPx)
        {
            Segments = segments;
            TotalLengthPx = totalLengthPx;
        }
    }
}
