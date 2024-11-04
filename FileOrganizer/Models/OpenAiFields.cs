using System.Text.Json.Serialization;

namespace FileOrganizer.Models;

public class OpenAiFields
{
    [JsonPropertyName("ModelId")]
    public string ModelId { get; set; }

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; }
}