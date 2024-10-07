using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public class Step4ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;

    public Step4ViewModel(AnalysisViewModel parent, MainWindow window, AppDbContext appDbContext)
    {
        StepNumber = 4;
        Icon = "fa-brain";
        Description = "Step 4";
        _parent = parent;
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