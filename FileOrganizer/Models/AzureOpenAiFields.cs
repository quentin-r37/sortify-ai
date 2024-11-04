using System;
using System.Text.Json.Serialization;

namespace FileOrganizer.Models;

public class AzureOpenAiFields
{
    [JsonPropertyName("DeploymentName")]
    public string DeploymentName { get; set; }

    [JsonPropertyName("Endpoint")]
    public Uri Endpoint { get; set; }

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; }

    [JsonPropertyName("ModelId")]
    public string ModelId { get; set; }
}