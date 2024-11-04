using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Models;
using FileOrganizer.Services;
using FileOrganizer.Shared;
using FileOrganizer.UIService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace FileOrganizer.ViewModels;

public partial class AnalysisViewModel : ViewModelBase
{
    public Analysis Model { get; set; }
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    public ITextEmbeddingGenerationService TextEmbeddingGenerationService { get; set; }
    public IChatCompletionService ChatCompletionService { get; set; }

    public string EmbeddingModelName { get; set; }

    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private bool _isEditing;
    private AIServiceConfiguration _aiServiceConfiguration;

    public AnalysisViewModel(Analysis analysis,
       MainWindowViewModel mainWindowViewModel, INotificationService notificationService,
        IViewModelFactory viewModelFactory, IServiceProvider serviceProvider)
    {

        Model = analysis;
        _mainWindowViewModel = mainWindowViewModel;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
        Step1 = viewModelFactory.CreateStep1ViewModel(this);
        Step1.Path = analysis.Path;
        Step2 = viewModelFactory.CreateStep2ViewModel(this);
        Step3 = viewModelFactory.CreateStep3ViewModel(this);
        Step4 = viewModelFactory.CreateStep4ViewModel(this);
        Steps = [Step1, Step2, Step3, Step4];
        CurrentStep = 1;
        _ = InitVectorCollectionAsync();
        UpdateStepStates();
    }

    public async Task InitVectorCollectionAsync()
    {
        //var connection = new SqliteConnection("Data Source=vectors.db");
        //connection.Open();
        var vectorStore = new InMemoryVectorStore();
        VectorCollection = vectorStore.GetCollection<string, FileVectorStoreRecord>($"embeddings_{Model.Id}");
        await VectorCollection.CreateCollectionIfNotExistsAsync();

        var configuration = _serviceProvider.GetRequiredService<IAIConfigurationService>();
        _aiServiceConfiguration = await configuration.LoadConfiguration();
        switch (_aiServiceConfiguration.EmbeddingService.SelectedProvider)
        {
            case ServiceProviders.Ollama:
                {
                    var ollamaAiFields = _aiServiceConfiguration.EmbeddingService.OllamaFields;
                    TextEmbeddingGenerationService = new OllamaTextEmbeddingGenerationService(ollamaAiFields.ModelId, ollamaAiFields.OllamaUrl);
                    break;
                }
            case ServiceProviders.AzureOpenAI:
                {
                    var azOpenAiFields = _aiServiceConfiguration.EmbeddingService.AzureOpenAiFields;
                    TextEmbeddingGenerationService = new AzureOpenAITextEmbeddingGenerationService(azOpenAiFields.DeploymentName, azOpenAiFields.Endpoint.AbsoluteUri, azOpenAiFields.ApiKey);
                    break;
                }
            case ServiceProviders.OpenAI:
                {
                    var openAiFields = _aiServiceConfiguration.EmbeddingService.OpenAiFields;
                    TextEmbeddingGenerationService = new OpenAITextEmbeddingGenerationService(openAiFields.ModelId, openAiFields.ApiKey);
                    break;
                }
        }

        switch (_aiServiceConfiguration.CompletionService.SelectedProvider)
        {
            case ServiceProviders.Ollama:
                {
                    var ollamaAiFields = _aiServiceConfiguration.CompletionService.OllamaFields;
                    ChatCompletionService = new OllamaChatCompletionService(ollamaAiFields.ModelId, ollamaAiFields.OllamaUrl);
                    break;
                }
            case ServiceProviders.AzureOpenAI:
                {
                    var azOpenAiFields = _aiServiceConfiguration.CompletionService.AzureOpenAiFields;
                    ChatCompletionService = new AzureOpenAIChatCompletionService(azOpenAiFields.DeploymentName, azOpenAiFields.Endpoint.AbsoluteUri, azOpenAiFields.ApiKey);
                    break;
                }
            case ServiceProviders.OpenAI:
                {
                    var openAiFields = _aiServiceConfiguration.CompletionService.OpenAiFields;
                    ChatCompletionService = new OpenAIChatCompletionService(openAiFields.ModelId, openAiFields.ApiKey);
                    break;
                }
        }

    }

    public IVectorStoreRecordCollection<string, FileVectorStoreRecord> VectorCollection { get; set; }

    public List<string> Keys { get; set; } = [];

    public ObservableCollection<StepViewModel> Steps { get; }

    public ObservableObject CurrentStepViewModel
    {
        get
        {
            return CurrentStep switch
            {
                1 => Step1,
                2 => Step2,
                3 => Step3,
                4 => Step4,
                _ => null
            };
        }
    }

    public string AnalysisName
    {
        get => Model.Name;
        set
        {
            if (Model.Name == value) return;
            Model.Name = value;
            OnPropertyChanged();
        }
    }

    public string AnalysisPath
    {
        get => Model.Path;
        set
        {
            if (Model.Path == value) return;
            Model.Path = value;
            OnPropertyChanged();
        }
    }

    public Step1ViewModel Step1 { get; }
    public Step2ViewModel Step2 { get; }
    public Step3ViewModel Step3 { get; }
    public Step4ViewModel Step4 { get; }

    partial void OnCurrentStepChanged(int value)
    {
        UpdateStepStates();
    }

    private void UpdateStepStates()
    {
        foreach (var step in Steps)
        {
            step.IsCurrentStep = step.StepNumber == CurrentStep;
            step.IsCompleted = step.StepNumber < CurrentStep;
            step.IsNotLastStep = step.StepNumber != Steps.Count;
        }
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task SaveName()
    {
        IsEditing = false;
        await _mainWindowViewModel.SaveNameAsync(this);
        _notificationService.ShowNotification("Operation succeeded", "Analysis saved successfully", NotificationType.Success);
    }

    [RelayCommand]
    public void Next()
    {
        NextStep();
    }

    [RelayCommand]
    public async Task Delete()
    {
        await _mainWindowViewModel.DeleteAnalyseAsync(this);
    }

    [RelayCommand]
    public void Previous()
    {
        PreviousStep();
    }

    public void NextStep()
    {
        if (CurrentStep < 4)
        {
            CurrentStep++;
            OnPropertyChanged(nameof(CurrentStep));
            OnPropertyChanged(nameof(CurrentStepViewModel));
        }
    }

    public void PreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            OnPropertyChanged(nameof(CurrentStep));
            OnPropertyChanged(nameof(CurrentStepViewModel));
        }
    }
}