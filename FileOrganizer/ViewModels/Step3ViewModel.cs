using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Shared;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public partial class Step3ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    private OrganizationType _selectedOrganizationType;
    public OrganizationType SelectedOrganizationType
    {
        get => _selectedOrganizationType;
        set => SetProperty(ref _selectedOrganizationType, value);
    }


    public Step3ViewModel(AnalysisViewModel parent, MainWindow? window)
    {
        _parent = parent;
    }

    [RelayCommand]
    private void Next()
    {
        _parent.Step4.PreparePreview(SelectedOrganizationType);
        _parent.NextStep();
    }

    [RelayCommand]
    private void Previous()
    {
        _parent.PreviousStep();
    }
}