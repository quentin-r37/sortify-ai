using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Models;
using FileOrganizer.Services;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FileOrganizer.ViewModels.SettingsWindowViewModel;

namespace FileOrganizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MainWindow _mainWindow;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IServiceProvider _serviceProvider;
    private ObservableCollection<AnalysisViewModel> _analyses;
    private readonly WindowNotificationManager _notificationManager;

    [ObservableProperty] private AnalysisViewModel _selectedAnalysis;
    [ObservableProperty] private string _embeddingModelProvider;
    [ObservableProperty] private string _embeddingModelName;
    [ObservableProperty] private string _completionModelProvider;
    [ObservableProperty] private string _completionModelName;


    [ObservableProperty] private SolidColorBrush _primaryBrush = new SolidColorBrush(new Color(255,40,40,40),1) ;
    [ObservableProperty] private SolidColorBrush _secondaryBrush = new SolidColorBrush(new Color(255,20,20,20),1) ;



    public ObservableCollection<AnalysisViewModel> Analyses
    {
        get => _analyses;
        set => SetProperty(ref _analyses, value);
    }

    public void UpdateWindowState(bool isActive)
    {
        PrimaryBrush = isActive ? new SolidColorBrush(Colors.Black,0.5) :  new SolidColorBrush(new Color(255,40,40,40),1) ;
        SecondaryBrush = isActive ? new SolidColorBrush(Colors.Black,0.3) :  new SolidColorBrush(new Color(255,20,20,20),1) ;

    }

    private async Task InitConfiguration()
    {
        var configurationService = _serviceProvider.GetRequiredService<IAIConfigurationService>();
        var aiServiceConfiguration = await configurationService.LoadConfiguration();
        EmbeddingModelProvider = aiServiceConfiguration.EmbeddingService.SelectedProvider;
        EmbeddingModelName = EmbeddingModelProvider switch
        {
            ServiceProviders.AzureOpenAI => aiServiceConfiguration.EmbeddingService.AzureOpenAiFields.ModelId,
            ServiceProviders.OpenAI => aiServiceConfiguration.EmbeddingService.OpenAiFields.ModelId,
            ServiceProviders.Ollama => aiServiceConfiguration.EmbeddingService.OllamaFields.ModelId,
            _ => EmbeddingModelName
        };

        CompletionModelProvider = aiServiceConfiguration.CompletionService.SelectedProvider;
        CompletionModelName = EmbeddingModelProvider switch
        {
            ServiceProviders.AzureOpenAI => aiServiceConfiguration.CompletionService.AzureOpenAiFields.ModelId,
            ServiceProviders.OpenAI => aiServiceConfiguration.CompletionService.OpenAiFields.ModelId,
            ServiceProviders.Ollama => aiServiceConfiguration.CompletionService.OllamaFields.ModelId,
            _ => CompletionModelName
        };
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
        _ = InitConfiguration();
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
        var model = _serviceProvider.GetRequiredService<SettingsWindowViewModel>();
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