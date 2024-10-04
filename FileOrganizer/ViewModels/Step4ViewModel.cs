using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Models;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public class Step4ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    public RelayCommand ExecuteCommand { get; }

    public Step4ViewModel(AnalysisViewModel parent, MainWindow? window)
    {
        _parent = parent;
        ExecuteCommand = new RelayCommand(ExecuteOrganization);
    }

    public void PreparePreview(OrganizationType organizationType)
    {
        // Prepare the preview based on the selected organization type
    }

    private void ExecuteOrganization()
    {
        // Perform the file organization
        // Save the analysis to the database
    }
}