using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Models;
using FileOrganizer.Services;
using FileOrganizer.Shared;
using FileOrganizer.UIService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;

namespace FileOrganizer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IServiceProvider _serviceProvider;
    private ObservableCollection<AnalysisViewModel> _analyses;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private bool _isLightTheme;
    [ObservableProperty] private SolidColorBrush _primaryBrush = new(new Color(255, 40, 40, 40), 1);
    [ObservableProperty] private SolidColorBrush _secondaryBrush = new(new Color(255, 20, 20, 20), 1);

    public string Theme => IsLightTheme ? "Light" : "Dark";

    partial void OnIsLightThemeChanged(bool value)
    {
        OnPropertyChanged(nameof(Theme));
        UpdateThemeBrushes();
    }

    private void UpdateThemeBrushes()
    {
        if (IsLightTheme)
        {
            PrimaryBrush = new SolidColorBrush(Colors.White, 0.8);
            SecondaryBrush = new SolidColorBrush(Colors.White, 0.6);
        }
        else
        {
            PrimaryBrush = new SolidColorBrush(Colors.Black, 0.7);
            SecondaryBrush = new SolidColorBrush(Colors.Black, 0.4);
        }
    }

    public void UpdateWindowState(bool isActive)
    {

        if (IsLightTheme)
        {
            PrimaryBrush = isActive
                ? new SolidColorBrush(Colors.White, 0.8)
                : new SolidColorBrush(Colors.White, 1);

            SecondaryBrush = isActive
                ? new SolidColorBrush(Colors.White, 0.6)
                : new SolidColorBrush(new Color(255, 240, 240, 240), 1);
        }
        else
        {
            PrimaryBrush = isActive
                ? new SolidColorBrush(Colors.Black, 0.7)
                : new SolidColorBrush(new Color(255, 40, 40, 40), 1);

            SecondaryBrush = isActive
                ? new SolidColorBrush(Colors.Black, 0.4)
                : new SolidColorBrush(new Color(255, 20, 20, 20), 1);
        }
       

    }

    [ObservableProperty] private AnalysisViewModel _selectedAnalysis;
    [ObservableProperty] private string _embeddingModelProvider;
    [ObservableProperty] private string _embeddingModelName;
    [ObservableProperty] private string _completionModelProvider;
    [ObservableProperty] private string _completionModelName;


    public ObservableCollection<AnalysisViewModel> Analyses
    {
        get => _analyses;
        set => SetProperty(ref _analyses, value);
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

    public MainWindowViewModel(IViewModelFactory viewModelFactory, IServiceProvider serviceProvider, IDialogService dialogService, INotificationService notificationService)
    {
        _viewModelFactory = viewModelFactory;
        _serviceProvider = serviceProvider;
        _dialogService = dialogService;
        _notificationService = notificationService;
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
            Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        var newAnalysisViewModel = _viewModelFactory.CreateAnalysisViewModel(newAnalysisModel, this, _notificationService);
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        dbContext.Analyses.Add(newAnalysisModel);
        await dbContext.SaveChangesAsync();

        Analyses.Add(newAnalysisViewModel);
        SelectedAnalysis = newAnalysisViewModel;
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        var model = _serviceProvider.GetRequiredService<SettingsWindowViewModel>();
        await _dialogService.ShowDialogAsync(model);
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
        return dbContext.Analyses.Select(analysis => _viewModelFactory.CreateAnalysisViewModel(analysis, currentModel, _notificationService));
    }
}