using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiveChartsCore.SkiaSharpView.Painting;
using Avalonia.Threading;

namespace FileOrganizer.ViewModels;

public partial class Step3ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    [ObservableProperty]
    private OrganizationType _selectedOrganizationType;

    [ObservableProperty]
    private string _searchQuery;

    partial void OnSearchQueryChanged(string value)
    {
        _ = PerformSearchAsync(value);
    }
    private async Task PerformSearchAsync(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var relevanceByPath = new Dictionary<string, double>();
            var itemsByPath = new Dictionary<string, FileItemViewModel>();

            await foreach (var r in _parent.Memory.SearchAsync("pdf_documents", value, 20, 0D, withEmbeddings: true))
            {
                var path = r.Metadata.AdditionalMetadata;
                var item = AllFiles.FirstOrDefault(model => model.Model.Path == path);
                if (item == null) continue;
                if (relevanceByPath.ContainsKey(path))
                    relevanceByPath[path] += r.Relevance;
                else
                {
                    relevanceByPath[path] = r.Relevance;
                    itemsByPath[path] = item;
                }
            }

            var matchingItems = itemsByPath.Values.ToList();
            foreach (var item in matchingItems)
            {
                item.Relevance = relevanceByPath[item.Model.Path];
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FilteredFiles = new ObservableCollection<FileItemViewModel>(matchingItems);
                OnPropertyChanged(nameof(FilteredFiles));
            });
        }
        else
        {
            foreach (var fileItemViewModel in AllFiles)
            {
                fileItemViewModel.Relevance = 0;
            }
            FilteredFiles = new ObservableCollection<FileItemViewModel>(AllFiles);
            OnPropertyChanged(nameof(FilteredFiles));
        }
    }

    public ObservableCollection<FileItemViewModel> FilteredFiles { get; set; }
    public ObservableCollection<FileItemViewModel> AllFiles { get; set; }

    public Step3ViewModel(AnalysisViewModel parent, MainWindow window, IServiceProvider serviceProvider)
    {
        _parent = parent;
        StepNumber = 3;
        Icon = "fa-magnifying-glass";
        Description = "Step 3";
    }

    public void LoadFileList(List<FileItemViewModel> files)
    {
        AllFiles = new ObservableCollection<FileItemViewModel>(files);
        FilteredFiles = new ObservableCollection<FileItemViewModel>(files);
        OnPropertyChanged(nameof(FilteredFiles));
    }

    [RelayCommand]
    private void Next()
    {
        _parent.Step4.PreparePreview(SelectedOrganizationType);
        _parent.NextStep();
    }

    [RelayCommand]
    private void Previous()
    {
        _parent.PreviousStep();
    }
}