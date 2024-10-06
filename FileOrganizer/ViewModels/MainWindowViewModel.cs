using System;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly AppDbContext _appDbContext;
        private ObservableCollection<AnalysisViewModel> _analyses;
        public ObservableCollection<AnalysisViewModel> Analyses
        {
            get => _analyses;
            set => SetProperty(ref _analyses, value);
        }

        [ObservableProperty]
        private AnalysisViewModel _selectedAnalysis;

        private readonly MainWindow _mainWindow;

        public MainWindowViewModel(MainWindow window, IViewModelFactory viewModelFactory, AppDbContext appDbContext)
        {
            _mainWindow = window;
            _viewModelFactory = viewModelFactory;
            _appDbContext = appDbContext;
            _analyses = [];
            _selectedAnalysis = null;
            Analyses = new ObservableCollection<AnalysisViewModel>(LoadAnalysesFromDatabaseAsync());
        }


        public MainWindowViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
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
            var newAnalysisViewModel = _viewModelFactory.CreateAnalysisViewModel(newAnalysisModel, _mainWindow);

            _appDbContext.Analyses.Add(newAnalysisModel);
            await _appDbContext.SaveChangesAsync();

            Analyses.Add(newAnalysisViewModel);
            SelectedAnalysis = newAnalysisViewModel;
        }

        private IEnumerable<AnalysisViewModel> LoadAnalysesFromDatabaseAsync()
        {
            var list = _appDbContext.Analyses
                .Select(analysis => _viewModelFactory.CreateAnalysisViewModel(analysis, _mainWindow))
                .ToList();

            return list;
        }
    }
}
