using System.Text.Json.Serialization;

namespace FileOrganizer.Models;

public class Service
{
    [JsonPropertyName("SelectedProvider")]
    public string SelectedProvider { get; set; }

    [JsonPropertyName("Providers")]
    public string[] Providers { get; set; }

    [JsonPropertyName("OllamaFields")]
    public OllamaFields OllamaFields { get; set; }

    [JsonPropertyName("AzureOpenAIFields")]
    public AzureOpenAiFields AzureOpenAiFields { get; set; }

    [JsonPropertyName("OpenAIFields")]
    public OpenAiFields OpenAiFields { get; set; }
}