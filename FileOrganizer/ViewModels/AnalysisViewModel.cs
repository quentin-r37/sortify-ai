using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System;
using FileOrganizer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Embeddings;
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace FileOrganizer.ViewModels;

public partial class AnalysisViewModel : ObservableObject
{
    public Analysis Model { get; set; }
    private readonly MainWindow _window;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly WindowNotificationManager _notificationManager;
    private readonly IServiceProvider _serviceProvider;

    public SemanticTextMemory Memory { get; set; }

    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private bool _isEditing;

    public AnalysisViewModel(Analysis analysis,
        MainWindow window, MainWindowViewModel mainWindowViewModel, WindowNotificationManager notificationManager,
        IViewModelFactory viewModelFactory, IServiceProvider serviceProvider)
    {

        Model = analysis;
        _window = window;
        _mainWindowViewModel = mainWindowViewModel;
        _notificationManager = notificationManager;
        _serviceProvider = serviceProvider;
        Step1 = viewModelFactory.CreateStep1ViewModel(this, window);
        Step1.Path = analysis.Path;
        Step2 = viewModelFactory.CreateStep2ViewModel(this, window);
        Step3 = viewModelFactory.CreateStep3ViewModel(this, window);
        Step4 = viewModelFactory.CreateStep4ViewModel(this, window);

        Steps = [Step1, Step2, Step3, Step4];
        CurrentStep = 1;
        _ = InitMemoryAsync();
        UpdateStepStates();
    }

    public async Task InitMemoryAsync()
    {
        var configuration = _serviceProvider.GetRequiredService<IAIConfigurationService>();
        var config = await configuration.LoadConfiguration();
        if (config.EmbeddingService.SelectedProvider == "Ollama")
        {
            var textEmbeddingService = new OllamaTextEmbeddingGenerationService("all-minilm", new Uri("http://localhost:11434"));
            Memory = new SemanticTextMemory(new VolatileMemoryStore(), textEmbeddingService);
        }
     
    }

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
        _notificationManager.Show("Analysis saved successfully", NotificationType.Success);

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