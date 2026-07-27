using System.IO;
using System.Text.Json;

namespace PathSearch.Parameter
{
    /// <summary>
    /// data/parameter.json 파일을 읽어 <see cref="Parameters"/> 객체로 역직렬화한다.
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
    }
}
