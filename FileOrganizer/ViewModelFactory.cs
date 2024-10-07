using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;

namespace FileOrganizer;

public class ViewModelFactory(AppDbContext dbContext) : IViewModelFactory
{
    public AnalysisViewModel CreateAnalysisViewModel(Analysis analysis, MainWindow window)
    {
        return new AnalysisViewModel(analysis, window, this);
    }

    public Step1ViewModel CreateStep1ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step1ViewModel(analysisViewModel, window, dbContext);
    }

    public Step2ViewModel CreateStep2ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step2ViewModel(analysisViewModel, window, dbContext);
    }

    public Step3ViewModel CreateStep3ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step3ViewModel(analysisViewModel, window, dbContext);
    }

    public Step4ViewModel CreateStep4ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step4ViewModel(analysisViewModel, window, dbContext);
    }
}