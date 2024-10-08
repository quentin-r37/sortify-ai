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
using FileOrganizer.Shared;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppDbContext _appDbContext;
    private readonly MainWindow _mainWindow;
    private readonly IViewModelFactory _viewModelFactory;
    private ObservableCollection<AnalysisViewModel> _analyses;
    private readonly WindowNotificationManager _notificationManager;

    [ObservableProperty] private AnalysisViewModel _selectedAnalysis;
    public ObservableCollection<AnalysisViewModel> Analyses
    {
        get => _analyses;
        set => SetProperty(ref _analyses, value);
    }

    public MainWindowViewModel(MainWindow window, IViewModelFactory viewModelFactory, AppDbContext appDbContext)
    {
        _mainWindow = window;
        _viewModelFactory = viewModelFactory;
        _appDbContext = appDbContext;
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

        _appDbContext.Analyses.Add(newAnalysisModel);
        await _appDbContext.SaveChangesAsync();

        Analyses.Add(newAnalysisViewModel);
        SelectedAnalysis = newAnalysisViewModel;
    }

    public async Task DeleteAnalyseAsync(AnalysisViewModel analysis)
    {
        _appDbContext.Analyses.Remove(analysis.Model);
        await _appDbContext.SaveChangesAsync();

        Analyses.Remove(analysis);
        var first = Analyses.FirstOrDefault();
        if (first != null) SelectedAnalysis = first;
    }

    public async Task SaveNameAsync(AnalysisViewModel analysis)
    {
        var entry = _appDbContext.Analyses.Entry(analysis.Model);
        entry.Entity.Name = analysis.AnalysisName;
        await _appDbContext.SaveChangesAsync();
    }

    private IEnumerable<AnalysisViewModel> LoadAnalysesFromDatabaseAsync()
    {
        var currentModel = this;
        return _appDbContext.Analyses.Select(analysis => _viewModelFactory.CreateAnalysisViewModel(analysis, _mainWindow, currentModel, _notificationManager));
    }
}