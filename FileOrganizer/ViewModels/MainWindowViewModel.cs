using System;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FileOrganizer.Models;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<AnalysisViewModel> _analyses;
        public ObservableCollection<AnalysisViewModel> Analyses
        {
            get => _analyses;
            set => SetProperty(ref _analyses, value);
        }

        [ObservableProperty]
        private AnalysisViewModel _selectedAnalysis;
   
        private readonly MainWindow _mainWindow;

        public MainWindowViewModel(MainWindow window)
        {
            _mainWindow = window;
            _analyses = [];
            _selectedAnalysis = null;
            Analyses = new ObservableCollection<AnalysisViewModel>(LoadAnalysesFromDatabase());
        }

        public MainWindowViewModel()
        {
        }

        [RelayCommand]
        private void CreateNewAnalysis()
        {
            var newAnalysisModel = new Analysis
            {
                Name = "New Analysis",
                CreatedDate = DateTime.Now
            };
            var newAnalysisViewModel = new AnalysisViewModel(newAnalysisModel, _mainWindow);
            Analyses.Add(newAnalysisViewModel);
            SelectedAnalysis = newAnalysisViewModel;
        }

        private IEnumerable<AnalysisViewModel> LoadAnalysesFromDatabase()
        {
            return new List<AnalysisViewModel>();
        }
    }
}
