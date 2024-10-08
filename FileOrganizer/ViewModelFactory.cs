using System;
using Avalonia.Controls.Notifications;
using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;

namespace FileOrganizer;

public class ViewModelFactory(IServiceProvider serviceProvider) : IViewModelFactory
{
    public AnalysisViewModel CreateAnalysisViewModel(Analysis analysis, MainWindow window, MainWindowViewModel mainWindowViewModel, WindowNotificationManager notificationManager)
    {
        return new AnalysisViewModel(analysis, window, mainWindowViewModel, notificationManager, this);
    }

    public Step1ViewModel CreateStep1ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step1ViewModel(analysisViewModel, window, serviceProvider);
    }

    public Step2ViewModel CreateStep2ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step2ViewModel(analysisViewModel, window, serviceProvider);
    }

    public Step3ViewModel CreateStep3ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step3ViewModel(analysisViewModel, window, serviceProvider);
    }

    public Step4ViewModel CreateStep4ViewModel(AnalysisViewModel analysisViewModel, MainWindow window)
    {
        return new Step4ViewModel(analysisViewModel, window, serviceProvider);
    }
}