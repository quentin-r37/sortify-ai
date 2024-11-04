using CommunityToolkit.Mvvm.ComponentModel;

namespace FileOrganizer.ViewModels;

public partial class StepViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isCompleted;

    [ObservableProperty] private bool _isCurrentStep;

    [ObservableProperty] private bool _isNotLastStep;

    public int StepNumber { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
}