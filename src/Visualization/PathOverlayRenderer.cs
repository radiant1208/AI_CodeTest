using System;
using System.Collections.Generic;
using OpenCvSharp;
using PathSearch.Planning;

namespace PathSearch.Visualization
{
    /// <summary>경로(HybridState 목록)를 이미지 위에 그리는 순수 렌더링 로직(파일 I/O 없음).</summary>
    public static class PathOverlayRenderer
    {
        private static readonly Scalar ForwardColor = new(255, 128, 0);
        private static readonly Scalar ReverseColor = new(0, 0, 255);
        private static readonly Scalar HeadingColor = new(0, 200, 255);
        private const int LineThickness = 2;
        private const int NodeRadius = 2;
        private const double HeadingTickLength = 10.0;

        /// <summary>원본 이미지를 복제해 경로 세그먼트(전진=파랑/후진=빨강)와 각 노드의 heading 틱(주황)을 그린 새 Mat을 반환한다.</summary>
        public static Mat Render(Mat original, IReadOnlyList<HybridState> path)
        {
            ArgumentNullException.ThrowIfNull(original);
            ArgumentNullException.ThrowIfNull(path);

            Mat overlay = original.Clone();

            for (int i = 1; i < path.Count; i++)
            {
                HybridState prev = path[i - 1];
                HybridState curr = path[i];
                Point p1 = new((int)Math.Round(prev.X), (int)Math.Round(prev.Y));
                Point p2 = new((int)Math.Round(curr.X), (int)Math.Round(curr.Y));
                Scalar color = curr.IsReverse ? ReverseColor : ForwardColor;
                Cv2.Line(overlay, p1, p2, color, LineThickness, LineTypes.AntiAlias);
            }

            foreach (HybridState state in path)
            {
                Point center = new((int)Math.Round(state.X), (int)Math.Round(state.Y));
                Cv2.Circle(overlay, center, NodeRadius, ForwardColor, thickness: -1, lineType: LineTypes.AntiAlias);

                Point headingTip = new(
                    (int)Math.Round(state.X + (HeadingTickLength * Math.Cos(state.ThetaRad))),
                    (int)Math.Round(state.Y + (HeadingTickLength * Math.Sin(state.ThetaRad))));
                Cv2.Line(overlay, center, headingTip, HeadingColor, 1, LineTypes.AntiAlias);
            }

            return overlay;
        }
    }
}
