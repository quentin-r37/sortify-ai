using System.Text.Json.Serialization;

namespace FileOrganizer.Models;

public class AIServiceConfiguration
{
    [JsonPropertyName("EmbeddingService")]
    public Service EmbeddingService { get; set; }

    [JsonPropertyName("CompletionService")]
    public Service CompletionService { get; set; }
}