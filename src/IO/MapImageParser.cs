using System;
using OpenCvSharp;
using PathSearch.Map;

namespace PathSearch.IO
{
    /// <summary>시작/목표 위치 Pose: 좌표(px)와 heading(rad)</summary>
    public readonly record struct MapPose(double X, double Y, double HeadingRad);

    /// <summary>맵 파싱 결과: 점유 격자 + 시작/목표 Pose</summary>
    public sealed class MapParseResult
    {
        public required OccupancyGrid Grid { get; init; }
        public required MapPose Start { get; init; }
        public required MapPose Goal { get; init; }
    }

    /// <summary>맵 이미지(Mat)를 색상 기반으로 분석해 OccupancyGrid와 시작/목표 Pose를 추출한다.</summary>
    public static class MapImageParser
    {
        /// <summary>그레이스케일 이진화 임계값(장애물 ~40, 이동 가능 영역 ~245의 중간값)</summary>
        private const int ObstacleGrayThreshold = 128;

        // 시작(초록)/목표(빨강) 마커는 절대 색상 범위가 아닌 채널 우세(dominance) 차이로 판별한다.
        // 절대 범위(InRange 박스)는 장애물↔배경 경계의 중립 회색(R=G=B) 안티에일리어싱 픽셀까지 오검출한다.
        // 실측 코어 색상: 초록 BGR(48,176,17) → G-R=159,G-B=128 / 빨강 BGR(27,27,197) → R-G=170,R-B=170
        private const int MarkerDominanceMargin = 60;

        /// <summary>마커 중심 주변을 이동 가능 영역으로 강제 처리할 반경(단위: px, 마커 반지름 약 16px보다 여유있게 설정)</summary>
        private const int MarkerClearRadius = 20;

        // 이미지에는 heading 정보가 없으므로 기본값 0 rad으로 설정한다. 추후 외부 입력으로 override할 수 있도록 MapPose에 HeadingRad 필드를 별도로 둔다.
        private const double DefaultHeadingRad = 0.0;

        /// <summary>맵 이미지를 이진화하여 OccupancyGrid를 만들고, 색상 기반으로 시작/목표 Pose를 추출한다.</summary>
        public static MapParseResult Parse(Mat image)
        {
            ArgumentNullException.ThrowIfNull(image);
            if (image.Empty())
            {
                throw new ArgumentException("빈 Mat은 파싱할 수 없습니다.", nameof(image));
            }

            if (image.Channels() != 3)
            {
                throw new ArgumentException($"BGR 3채널 Mat이 필요합니다(입력 채널 수: {image.Channels()}).", nameof(image));
            }

            using Mat gray = new();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            using Mat binary = new();
            Cv2.Threshold(gray, binary, ObstacleGrayThreshold, 255, ThresholdTypes.BinaryInv);

            Mat[] channels = Cv2.Split(image);
            using Mat b = channels[0];
            using Mat g = channels[1];
            using Mat r = channels[2];

            using Mat greenMask = BuildDominanceMask(g, r, b);
            using Mat redMask = BuildDominanceMask(r, g, b);

            (double startX, double startY) = GetCentroid(greenMask, "시작점(초록)");
            (double goalX, double goalY) = GetCentroid(redMask, "목표점(빨강)");

            OccupancyGrid grid = BuildGrid(binary);
            ClearDisk(grid, startX, startY, MarkerClearRadius);
            ClearDisk(grid, goalX, goalY, MarkerClearRadius);

            return new MapParseResult
            {
                Grid = grid,
                Start = new MapPose(startX, startY, DefaultHeadingRad),
                Goal = new MapPose(goalX, goalY, DefaultHeadingRad),
            };
        }

        private static OccupancyGrid BuildGrid(Mat binary)
        {
            int width = binary.Width;
            int height = binary.Height;
            OccupancyGrid grid = new(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    grid[x, y] = binary.At<byte>(y, x) != 0;
                }
            }

            return grid;
        }

        // dominant 채널이 나머지 두 채널보다 MarkerDominanceMargin 이상 큰 픽셀만 마커로 판정한다.
        private static Mat BuildDominanceMask(Mat dominant, Mat other1, Mat other2)
        {
            using Mat diff1 = new();
            Cv2.Subtract(dominant, other1, diff1);
            using Mat diff2 = new();
            Cv2.Subtract(dominant, other2, diff2);

            using Mat mask1 = new();
            Cv2.Threshold(diff1, mask1, MarkerDominanceMargin, 255, ThresholdTypes.Binary);
            using Mat mask2 = new();
            Cv2.Threshold(diff2, mask2, MarkerDominanceMargin, 255, ThresholdTypes.Binary);

            Mat mask = new();
            try
            {
                Cv2.BitwiseAnd(mask1, mask2, mask);
                return mask;
            }
            catch
            {
                mask.Dispose();
                throw;
            }
        }

        private static (double X, double Y) GetCentroid(Mat mask, string label)
        {
            Moments m = Cv2.Moments(mask, true);
            if (m.M00 <= 0)
            {
                throw new InvalidOperationException($"맵 이미지에서 {label} 마커를 찾을 수 없습니다.");
            }

            return (m.M10 / m.M00, m.M01 / m.M00);
        }

        private static void ClearDisk(OccupancyGrid grid, double centerX, double centerY, int radius)
        {
            int minX = Math.Max(0, (int)(centerX - radius));
            int maxX = Math.Min(grid.Width - 1, (int)(centerX + radius));
            int minY = Math.Max(0, (int)(centerY - radius));
            int maxY = Math.Min(grid.Height - 1, (int)(centerY + radius));
            int radiusSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    double dx = x - centerX;
                    double dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        grid[x, y] = false;
                    }
                }
            }
        }
    }
}
