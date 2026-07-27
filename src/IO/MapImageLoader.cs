using System;
using System.IO;
using OpenCvSharp;
using PathSearch.Parameter;

namespace PathSearch.IO
{
    /// <summary>maps/ 폴더의 PNG 맵 이미지를 OpenCvSharp Mat으로 로드.</summary>
    public static class MapImageLoader
    {
        /// <summary>이미지를 BGR 컬러 Mat으로 로드하고 mapParameters에 정의된 크기를 검증한다.</summary>
        public static Mat Load(string imagePath, MapParameters mapParameters)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("맵 이미지 경로가 비어 있습니다.", nameof(imagePath));
            }

            ArgumentNullException.ThrowIfNull(mapParameters);

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"맵 이미지 파일을 찾을 수 없습니다: {imagePath}", imagePath);
            }

            Mat image = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (image.Empty())
            {
                throw new InvalidOperationException($"맵 이미지를 로드할 수 없습니다(형식 오류 가능): {imagePath}");
            }

            if (image.Width != mapParameters.Width || image.Height != mapParameters.Height)
            {
                string error = $"맵 이미지 크기가 올바르지 않습니다({image.Width}x{image.Height}, 기대값 {mapParameters.Width}x{mapParameters.Height}): {imagePath}";
                image.Dispose();
                throw new InvalidOperationException(error);
            }

            return image;
        }
    }
}
