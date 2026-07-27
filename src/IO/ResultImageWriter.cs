using System;
using System.IO;
using OpenCvSharp;

namespace PathSearch.IO
{
    /// <summary>렌더링된 결과 Mat을 results/result_{원본파일명}.png로 저장한다.</summary>
    public static class ResultImageWriter
    {
        /// <summary>resultDirectory에 result_{원본 맵 파일명}으로 저장하고 저장된 전체 경로를 반환한다.</summary>
        public static string Save(Mat resultImage, string originalMapPath, string resultDirectory)
        {
            ArgumentNullException.ThrowIfNull(resultImage);
            if (string.IsNullOrWhiteSpace(originalMapPath))
            {
                throw new ArgumentException("원본 맵 경로가 비어 있습니다.", nameof(originalMapPath));
            }
            if (string.IsNullOrWhiteSpace(resultDirectory))
            {
                throw new ArgumentException("결과 저장 폴더가 비어 있습니다.", nameof(resultDirectory));
            }

            Directory.CreateDirectory(resultDirectory);

            string originalFileName = Path.GetFileName(originalMapPath);
            string resultPath = Path.Combine(resultDirectory, $"result_{originalFileName}");

            Cv2.ImWrite(resultPath, resultImage);
            return resultPath;
        }
    }
}
