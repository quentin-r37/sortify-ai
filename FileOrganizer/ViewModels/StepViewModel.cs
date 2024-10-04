using CommunityToolkit.Mvvm.ComponentModel;

namespace FileOrganizer.ViewModels;

public partial class StepViewModel : ObservableObject
{
    public int StepNumber { get; set; }
    public string Icon { get; set; } 
    public string Description { get; set; }

    [ObservableProperty]
    private bool _isCurrentStep;

    [ObservableProperty]
    private bool _isCompleted;
}