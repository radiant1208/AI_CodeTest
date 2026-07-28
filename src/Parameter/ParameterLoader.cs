using System.IO;
using System.Text.Json;

namespace PathSearch.Parameter
{
    /// <summary>
    /// data/parameter.json 파일을 <see cref="Parameters"/> 객체로 역직렬화/직렬화한다.
    /// </summary>
    public static class ParameterLoader
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static Parameters Load(string parameterFilePath)
        {
            if (!File.Exists(parameterFilePath))
            {
                return new Parameters();
            }

            string json = File.ReadAllText(parameterFilePath);
            Parameters? parameters = JsonSerializer.Deserialize<Parameters>(json, SerializerOptions);

            return parameters ?? new Parameters();
        }

        /// <summary>parameters를 parameterFilePath에 저장한다(FE의 실시간 파라미터 수정을 영속화).</summary>
        public static void Save(string parameterFilePath, Parameters parameters)
        {
            string? directory = Path.GetDirectoryName(parameterFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(parameterFilePath, json);
        }
    }
}
