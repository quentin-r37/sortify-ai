using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FileOrganizer.ViewModels;

public class Step4ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;

    public Step4ViewModel(AnalysisViewModel parent, MainWindow window, IServiceProvider serviceProvider)
    {
        StepNumber = 4;
        Icon = "fa-brain";
        Description = "Step 4";
        _parent = parent;
    }

    public async Task PreparePreview(OrganizationType organizationType)
    {
       // var test = _parent.Memory.GetCollectionsAsync();


        // Prepare the preview based on the selected organization type
    }

    private void ExecuteOrganization()
    {
        // Perform the file organization
        // Save the analysis to the database
    }
}