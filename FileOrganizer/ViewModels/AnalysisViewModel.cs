using CommunityToolkit.Mvvm.ComponentModel;
using FileOrganizer.Models;
using FileOrganizer.Views;
using System.Collections.ObjectModel;
using System;

namespace FileOrganizer.ViewModels;
public partial class AnalysisViewModel : ObservableObject
{
    private readonly Analysis _model;

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
        }
    }

    public AnalysisViewModel(Analysis analysis, MainWindow window)
    {
        _model = analysis;
        Step1 = new Step1ViewModel(this, window)
        {
            StepNumber = 1,
            Icon = "fa-folder",
            Description = "Step 1"
        };
        Step2 = new Step2ViewModel(this, window)
        {
            StepNumber = 2,
            Icon = "fa-chart-simple",
            Description = "Step 2"
        };
        Step3 = new Step3ViewModel(this, window)
        {
            StepNumber = 3,
            Icon = "fa-magnifying-glass",
            Description = "Step 3"
        };
        Step4 = new Step4ViewModel(this, window)
        {
            StepNumber = 4,
            Icon = "fa-brain",
            Description = "Step 4"
        };

        Steps = [Step1, Step2, Step3, Step4];
        CurrentStep = 1;
        UpdateStepStates();
    }

    [ObservableProperty]
    private int _currentStep;

    public Step1ViewModel Step1 { get; }
    public Step2ViewModel Step2 { get; }
    public Step3ViewModel Step3 { get; }
    public Step4ViewModel Step4 { get; }

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
