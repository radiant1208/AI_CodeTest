using System;
using System.Collections.Generic;
using OpenCvSharp;
using PathSearch.Planning;
using PathSearch.Planning.Kinematics;

namespace PathSearch.Visualization
{
    /// <summary>경로(HybridState 목록)를 이미지 위에 그리는 순수 렌더링 로직(파일 I/O 없음).</summary>
    public static class PathOverlayRenderer
    {
        private static readonly Scalar ForwardColor = new(255, 128, 0);
        private static readonly Scalar ReverseColor = new(0, 0, 255);
        private static readonly Scalar HeadingColor = new(0, 200, 255);
        private static readonly Scalar FootprintColor = new(0, 255, 0);
        private const int LineThickness = 2;
        private const int NodeRadius = 2;
        private const double HeadingTickLength = 10.0;
        private const int FootprintLineThickness = 1;
        // 경로점마다 Footprint 사각형을 그리면 겹쳐서 알아보기 어려우므로 이 간격(노드 개수)마다만 그린다.
        private const int FootprintDrawInterval = 8;

        /// <summary>원본 이미지를 복제해 경로 세그먼트(전진=파랑/후진=빨강), 각 노드의 heading 틱(주황),
        /// 일정 간격(FootprintDrawInterval)마다 로봇 Footprint 사각형(녹색, 시작/끝 노드는 항상 포함)을 그린
        /// 새 Mat을 반환한다.</summary>
        public static Mat Render(Mat original, IReadOnlyList<HybridState> path, Footprint footprint)
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

            for (int i = 0; i < path.Count; i += FootprintDrawInterval)
            {
                DrawFootprint(overlay, path[i], footprint);
            }
            if (path.Count > 0 && (path.Count - 1) % FootprintDrawInterval != 0)
            {
                DrawFootprint(overlay, path[^1], footprint);
            }

            return overlay;
        }

        private static void DrawFootprint(Mat overlay, HybridState state, Footprint footprint)
        {
            (double X, double Y)[] corners = footprint.GetCorners(state.X, state.Y, state.ThetaRad);
            Point[] points = new Point[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                points[i] = new Point((int)Math.Round(corners[i].X), (int)Math.Round(corners[i].Y));
            }

            Cv2.Polylines(overlay, new[] { points }, isClosed: true, FootprintColor, FootprintLineThickness, LineTypes.AntiAlias);
        }
    }
}
