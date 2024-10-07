using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public partial class Step3ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    private OrganizationType _selectedOrganizationType;


    public Step3ViewModel(AnalysisViewModel parent, MainWindow window, AppDbContext appDbContext)
    {
        _parent = parent;
        StepNumber = 3;
        Icon = "fa-magnifying-glass";
        Description = "Step 3";
    }

    public OrganizationType SelectedOrganizationType
    {
        get => _selectedOrganizationType;
        set => SetProperty(ref _selectedOrganizationType, value);
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