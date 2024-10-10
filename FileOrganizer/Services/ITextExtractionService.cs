using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.ViewModels;
using Microsoft.SemanticKernel.Memory;
#pragma warning disable SKEXP0001

namespace FileOrganizer.Services;

public interface ITextExtractionService
{
    Task ExtractAndEmbedAsync(
        IEnumerable<FileItemViewModel> files,
        SemanticTextMemory memory,
        IProgress<(int Processed, int Total, string Message)> progress,
        CancellationToken cancellationToken = default);
}

public class AiServiceConfiguration
{
    [JsonPropertyName("EmbeddingService")]
    public Service EmbeddingService { get; set; }

    [JsonPropertyName("CompletionService")]
    public Service CompletionService { get; set; }
}

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

public class OllamaFields
{
    [JsonPropertyName("ModelId")]
    public string ModelId { get; set; }

    [JsonPropertyName("OllamaUrl")]
    public Uri OllamaUrl { get; set; }
}

public class OpenAiFields
{
    [JsonPropertyName("ModelId")]
    public string ModelId { get; set; }

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; }
}

