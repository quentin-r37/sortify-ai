using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Shared;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public partial class AnalysisViewModel : ObservableObject
{
    private readonly Analysis _model;

    [ObservableProperty] private int _currentStep;

    [ObservableProperty] private bool _isEditing;

    public AnalysisViewModel(Analysis analysis, MainWindow window, IViewModelFactory viewModelFactory)
    {
        _model = analysis;
        Step1 = viewModelFactory.CreateStep1ViewModel(this, window);
        Step1.Path = analysis.Path;
        Step2 = viewModelFactory.CreateStep2ViewModel(this, window);
        Step3 = viewModelFactory.CreateStep3ViewModel(this, window);
        Step4 = viewModelFactory.CreateStep4ViewModel(this, window);

        Steps = [Step1, Step2, Step3, Step4];
        CurrentStep = 1;
        UpdateStepStates();
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
        get => _model.Name;
        set
        {
            if (_model.Name == value) return;
            _model.Name = value;
            OnPropertyChanged();
        }
    }

    public string AnalysisPath
    {
        get => _model.Path;
        set
        {
            if (_model.Path == value) return;
            _model.Path = value;
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
    public void Next()
    {
        NextStep();
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