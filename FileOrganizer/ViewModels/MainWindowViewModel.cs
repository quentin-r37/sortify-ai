using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Services;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileOrganizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MainWindow _mainWindow;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IServiceProvider _serviceProvider;
    private ObservableCollection<AnalysisViewModel> _analyses;
    private readonly WindowNotificationManager _notificationManager;

    [ObservableProperty] private AnalysisViewModel _selectedAnalysis;
    public ObservableCollection<AnalysisViewModel> Analyses
    {
        get => _analyses;
        set => SetProperty(ref _analyses, value);
    }
    public MainWindowViewModel()
    {
    }

    public MainWindowViewModel(MainWindow window, IViewModelFactory viewModelFactory, IServiceProvider serviceProvider)
    {
        _mainWindow = window;
        _viewModelFactory = viewModelFactory;
        _serviceProvider = serviceProvider;
        _notificationManager = new WindowNotificationManager(TopLevel.GetTopLevel(window))
        {
            MaxItems = 3,
            Position = NotificationPosition.TopCenter
        };
        _analyses = [];
        _selectedAnalysis = null;
        Analyses = new ObservableCollection<AnalysisViewModel>(LoadAnalysesFromDatabaseAsync());
    }

    [RelayCommand]
    private async Task CreateNewAnalysis()
    {
        var newAnalysisModel = new Analysis
        {
            Name = "New Analysis",
            CreatedDate = DateTime.Now,
            Path = "C:\\Users\\quent\\Downloads"
        };
        var newAnalysisViewModel = _viewModelFactory.CreateAnalysisViewModel(newAnalysisModel, _mainWindow, this, _notificationManager);

        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        dbContext.Analyses.Add(newAnalysisModel);
        await dbContext.SaveChangesAsync();

        Analyses.Add(newAnalysisViewModel);
        SelectedAnalysis = newAnalysisViewModel;
    }


    [RelayCommand]
    private async Task OpenSettings()
    {
        var v = _serviceProvider.GetRequiredService<SettingsWindow>();
        var model= _serviceProvider.GetRequiredService<SettingsWindowViewModel>();
        model.RefWindow = v;
        v.Activate();
        v.DataContext = model;
        await v.ShowDialog(_mainWindow);
    }

    public async Task DeleteAnalyseAsync(AnalysisViewModel analysis)
    {
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        dbContext.Analyses.Remove(analysis.Model);
        await dbContext.SaveChangesAsync();

        Analyses.Remove(analysis);
        var first = Analyses.FirstOrDefault();
        if (first != null) SelectedAnalysis = first;
    }

    public async Task SaveNameAsync(AnalysisViewModel analysis)
    {
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        var entry = dbContext.Analyses.Entry(analysis.Model);
        entry.Entity.Name = analysis.AnalysisName;
        await dbContext.SaveChangesAsync();
    }

    private IEnumerable<AnalysisViewModel> LoadAnalysesFromDatabaseAsync()
    {
        var currentModel = this;
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        return dbContext.Analyses.Select(analysis => _viewModelFactory.CreateAnalysisViewModel(analysis, _mainWindow, currentModel, _notificationManager));
    }
}