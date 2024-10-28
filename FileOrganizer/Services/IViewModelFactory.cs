using FileOrganizer.Shared;
using FileOrganizer.UIService;
using FileOrganizer.ViewModels;

namespace FileOrganizer.Services;

public interface IViewModelFactory
{
    AnalysisViewModel CreateAnalysisViewModel(Analysis analysis, MainWindowViewModel mainWindowViewModel,
        INotificationService notificationService);
    Step1ViewModel CreateStep1ViewModel(AnalysisViewModel analysisViewModel);
    Step2ViewModel CreateStep2ViewModel(AnalysisViewModel analysisViewModel);
    Step3ViewModel CreateStep3ViewModel(AnalysisViewModel analysisViewModel);
    Step4ViewModel CreateStep4ViewModel(AnalysisViewModel analysisViewModel);
}