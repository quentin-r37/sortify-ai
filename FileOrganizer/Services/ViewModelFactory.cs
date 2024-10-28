using System;
using Avalonia.Controls.Notifications;
using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using FileOrganizer.Views;

namespace FileOrganizer.Services;

public class ViewModelFactory(IServiceProvider serviceProvider) : IViewModelFactory
{
    public AnalysisViewModel CreateAnalysisViewModel(Analysis analysis,  MainWindowViewModel mainWindowViewModel, WindowNotificationManager notificationManager)
    {
        return new AnalysisViewModel(analysis,  mainWindowViewModel, notificationManager, this, serviceProvider);
    }

    public Step1ViewModel CreateStep1ViewModel(AnalysisViewModel analysisViewModel)
    {
        return new Step1ViewModel(analysisViewModel, serviceProvider);
    }

    public Step2ViewModel CreateStep2ViewModel(AnalysisViewModel analysisViewModel)
    {
        return new Step2ViewModel(analysisViewModel, serviceProvider);
    }

    public Step3ViewModel CreateStep3ViewModel(AnalysisViewModel analysisViewModel)
    {
        return new Step3ViewModel(analysisViewModel, serviceProvider);
    }

    public Step4ViewModel CreateStep4ViewModel(AnalysisViewModel analysisViewModel)
    {
        return new Step4ViewModel(analysisViewModel, serviceProvider);
    }
}