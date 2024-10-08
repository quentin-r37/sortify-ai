using Avalonia.Controls.Notifications;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;

namespace FileOrganizer;

public interface IViewModelFactory
{
    AnalysisViewModel CreateAnalysisViewModel(Analysis analysis, MainWindow window, MainWindowViewModel mainWindowViewModel, WindowNotificationManager notificationManager);
    Step1ViewModel CreateStep1ViewModel(AnalysisViewModel analysisViewModel, MainWindow window);
    Step2ViewModel CreateStep2ViewModel(AnalysisViewModel analysisViewModel, MainWindow window);
    Step3ViewModel CreateStep3ViewModel(AnalysisViewModel analysisViewModel, MainWindow window);
    Step4ViewModel CreateStep4ViewModel(AnalysisViewModel analysisViewModel, MainWindow window);
}