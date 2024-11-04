using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Services;
using FileOrganizer.Shared;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable SKEXP0001

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
            var searchVector = await _parent.TextEmbeddingGenerationService.GenerateEmbeddingAsync(value);
            var searchResult = await _parent.VectorCollection.VectorizedSearchAsync(searchVector, new VectorSearchOptions()
            {
                Top = 50
            });


            await foreach (var r in searchResult.Results)
            {
                var path = r.Record.AdditionalMetadata;
                var item = AllFiles.FirstOrDefault(model => model.Model.Path == path);
                if (item == null) continue;
                if (relevanceByPath.ContainsKey(path))
                    relevanceByPath[path] += r.Score ?? 0;
                else
                {
                    relevanceByPath[path] = r.Score ?? 0;
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

    public Step3ViewModel(AnalysisViewModel parent, IServiceProvider serviceProvider)
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
    private async Task Next()
    {
        _parent.Step4.LoadFileList(AllFiles.WhereFileTypeIsDocument());
        _parent.NextStep();
        await _parent.Step4.PrepareOrganizeAsync(SelectedOrganizationType);

    }

    [RelayCommand]
    private void Previous()
    {
        _parent.PreviousStep();
    }
}