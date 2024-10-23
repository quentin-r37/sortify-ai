using System;
using System.Text.Json.Serialization;

namespace FileOrganizer.Models;

public class OllamaFields
{
    [JsonPropertyName("ModelId")]
    public string ModelId { get; set; }

    [JsonPropertyName("OllamaUrl")]
    public Uri OllamaUrl { get; set; }
}