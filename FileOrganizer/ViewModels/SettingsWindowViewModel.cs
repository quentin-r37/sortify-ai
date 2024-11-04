using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Models;
using FileOrganizer.Services;
using FileOrganizer.UIService;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001

namespace FileOrganizer.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly IAIConfigurationService _configurationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _selectedEmbeddingProvider = "Ollama";
    [ObservableProperty]
    private string _selectedCompletionProvider = "Ollama";
    [ObservableProperty]
    private string _embeddingModelId;
    [ObservableProperty]
    private string _embeddingApiKey;
    [ObservableProperty]
    private string _embeddingDeploymentName;
    [ObservableProperty]
    private string _embeddingEndpoint;
    [ObservableProperty]
    private bool? _embeddingTestPassed;

    [ObservableProperty]
    private string _completionModelId;
    [ObservableProperty]
    private string _completionApiKey;
    [ObservableProperty]
    private string _completionDeploymentName;
    [ObservableProperty]
    private string _completionEndpoint;
    [ObservableProperty]
    private bool? _completionTestPassed;

    [ObservableProperty] private SolidColorBrush _primaryBrush = new(new Color(255, 40, 40, 40), 1);
    [ObservableProperty] private SolidColorBrush _secondaryBrush = new(new Color(255, 20, 20, 20), 1);


    public ObservableCollection<string> Providers { get; set; } = ["Ollama", "Azure OpenAI", "OpenAI"];

    public SettingsWindowViewModel(IAIConfigurationService configurationService, IDialogService dialogService)
    {
        _configurationService = configurationService;
        _dialogService = dialogService;
        _ = InitConfigAsync();
    }

    public SettingsWindowViewModel()
    {

    }
    public void UpdateWindowState(bool isActive)
    {
        PrimaryBrush = isActive ? new SolidColorBrush(Colors.Black, 0.7) : new SolidColorBrush(new Color(255, 40, 40, 40), 1);
        SecondaryBrush = isActive ? new SolidColorBrush(Colors.Black, 0.4) : new SolidColorBrush(new Color(255, 20, 20, 20), 1);
    }
    private async Task InitConfigAsync()
    {
        var conf = await _configurationService.LoadConfiguration();
        SelectedEmbeddingProvider = conf.EmbeddingService.SelectedProvider;
        SelectedCompletionProvider = conf.CompletionService.SelectedProvider;

        switch (conf.EmbeddingService.SelectedProvider)
        {
            case ServiceProviders.Ollama:
                EmbeddingEndpoint = conf.EmbeddingService.OllamaFields.OllamaUrl.ToString();
                EmbeddingModelId = conf.EmbeddingService.OllamaFields.ModelId;
                break;
            case ServiceProviders.AzureOpenAI:
                EmbeddingEndpoint = conf.EmbeddingService.AzureOpenAiFields.Endpoint.ToString();
                EmbeddingModelId = conf.EmbeddingService.AzureOpenAiFields.ModelId;
                EmbeddingApiKey = conf.EmbeddingService.AzureOpenAiFields.ApiKey;
                EmbeddingDeploymentName = conf.EmbeddingService.AzureOpenAiFields.DeploymentName;
                break;
            case ServiceProviders.OpenAI:
                EmbeddingModelId = conf.EmbeddingService.OpenAiFields.ModelId;
                EmbeddingApiKey = conf.EmbeddingService.OpenAiFields.ApiKey;
                break;
        }

        switch (conf.CompletionService.SelectedProvider)
        {
            case ServiceProviders.Ollama:
                CompletionEndpoint = conf.CompletionService.OllamaFields.OllamaUrl.ToString();
                CompletionModelId = conf.CompletionService.OllamaFields.ModelId;
                break;
            case ServiceProviders.AzureOpenAI:
                CompletionEndpoint = conf.CompletionService.AzureOpenAiFields.Endpoint.ToString();
                CompletionModelId = conf.CompletionService.AzureOpenAiFields.ModelId;
                CompletionApiKey = conf.CompletionService.AzureOpenAiFields.ApiKey;
                CompletionDeploymentName = conf.CompletionService.AzureOpenAiFields.DeploymentName;
                break;
            case ServiceProviders.OpenAI:
                CompletionModelId = conf.CompletionService.OpenAiFields.ModelId;
                CompletionApiKey = conf.CompletionService.OpenAiFields.ApiKey;
                break;
        }

    }

    [RelayCommand]
    public async Task TestEmbedding()
    {
        var textEmbeddingService = CreateTextEmbeddingService();
        if (textEmbeddingService == null)
        {
            EmbeddingTestPassed = false;
            return;
        }
        var memory = new SemanticTextMemory(new VolatileMemoryStore(), textEmbeddingService);
        try
        {
            await memory.SaveInformationAsync("test", "test", "test", "test");
            EmbeddingTestPassed = true;
        }
        catch (Exception)
        {
            EmbeddingTestPassed = false;
        }
    }
    private ITextEmbeddingGenerationService CreateTextEmbeddingService()
    {
        return SelectedEmbeddingProvider switch
        {
            ServiceProviders.Ollama => new OllamaTextEmbeddingGenerationService(EmbeddingModelId,
                new Uri(EmbeddingEndpoint)),
            ServiceProviders.AzureOpenAI => new AzureOpenAITextEmbeddingGenerationService(EmbeddingDeploymentName,
                EmbeddingEndpoint, EmbeddingApiKey, EmbeddingModelId),
            ServiceProviders.OpenAI => new OpenAITextEmbeddingGenerationService(EmbeddingModelId,
                EmbeddingEndpoint),
            _ => null
        };
    }

    [RelayCommand]
    public async Task TestCompletion()
    {
        var textCompletionService = CreateTextCompletionService();
        if (textCompletionService == null)
        {
            CompletionTestPassed = false;
            return;
        }

        try
        {
            await textCompletionService.GetChatMessageContentsAsync(new ChatHistory("Only say one word"));
            CompletionTestPassed = true;
        }
        catch (Exception)
        {
            CompletionTestPassed = false;
        }
    }

    private IChatCompletionService CreateTextCompletionService()
    {
        return SelectedCompletionProvider switch
        {
            ServiceProviders.Ollama => new OllamaChatCompletionService(CompletionModelId, new Uri(CompletionEndpoint)),
            ServiceProviders.AzureOpenAI => new AzureOpenAIChatCompletionService(CompletionDeploymentName,
                CompletionEndpoint, CompletionApiKey, CompletionModelId),
            ServiceProviders.OpenAI => new OpenAIChatCompletionService(CompletionEndpoint, CompletionApiKey),
            _ => null
        };
    }

    [RelayCommand]
    public async Task SaveConfig()
    {
        var confToSave = new AIServiceConfiguration
        {
            CompletionService = new Service()
            {
                SelectedProvider = SelectedCompletionProvider,
                OllamaFields = new OllamaFields()
                {
                    OllamaUrl = new Uri(CompletionEndpoint),
                    ModelId = CompletionModelId
                },
                AzureOpenAiFields = new AzureOpenAiFields()
                {
                    ModelId = CompletionModelId,
                    Endpoint = new Uri(CompletionEndpoint),
                    ApiKey = CompletionApiKey,
                    DeploymentName = CompletionDeploymentName
                },
                OpenAiFields = new OpenAiFields()
                {
                    ModelId = CompletionModelId,
                    ApiKey = CompletionApiKey
                }
            },
            EmbeddingService = new Service()
            {
                SelectedProvider = SelectedEmbeddingProvider,
                OllamaFields = new OllamaFields()
                {
                    OllamaUrl = new Uri(EmbeddingEndpoint),
                    ModelId = EmbeddingModelId
                },
                AzureOpenAiFields = new AzureOpenAiFields()
                {
                    ModelId = EmbeddingModelId,
                    Endpoint = new Uri(EmbeddingEndpoint),
                    ApiKey = EmbeddingApiKey,
                    DeploymentName = EmbeddingDeploymentName
                },
                OpenAiFields = new OpenAiFields()
                {
                    ModelId = EmbeddingModelId,
                    ApiKey = EmbeddingApiKey
                }
            }
        };
        await _configurationService.SaveConfiguration(confToSave);

        _dialogService.CloseDialog(this);

    }


    partial void OnSelectedEmbeddingProviderChanged(string value)
    {
        OnPropertyChanged(nameof(ShowEmbeddedOllamaFields));
        OnPropertyChanged(nameof(ShowEmbeddedAzureFields));
        OnPropertyChanged(nameof(ShowEmbeddedOpenAIFields));
    }

    partial void OnSelectedCompletionProviderChanged(string value)
    {
        OnPropertyChanged(nameof(ShowCompletionOllamaFields));
        OnPropertyChanged(nameof(ShowCompletionAzureFields));
        OnPropertyChanged(nameof(ShowCompletionOpenAIFields));
    }

    public bool ShowEmbeddedOllamaFields => SelectedEmbeddingProvider == "Ollama";
    public bool ShowEmbeddedAzureFields => SelectedEmbeddingProvider == "Azure OpenAI";
    public bool ShowEmbeddedOpenAIFields => SelectedEmbeddingProvider == "OpenAI";

    public bool ShowCompletionOllamaFields => SelectedCompletionProvider == "Ollama";
    public bool ShowCompletionAzureFields => SelectedCompletionProvider == "Azure OpenAI";
    public bool ShowCompletionOpenAIFields => SelectedCompletionProvider == "OpenAI";

}