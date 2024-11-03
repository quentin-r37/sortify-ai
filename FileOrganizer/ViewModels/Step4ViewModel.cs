using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Models;
using FileOrganizer.Shared;
using FileOrganizer.UIService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileOrganizer.ViewModels;

public partial class Step4ViewModel : StepViewModel
{
    public ObservableCollection<FileItemViewModel> AllFiles { get; set; }
    public ObservableCollection<Node> CurrentStructure { get; set; }
    public ObservableCollection<Node> ProposedStructure { get; set; }
    private List<FileMove> FileMoves { get; set; }
    
    [ObservableProperty]
    private int _currentStructureNodesCount;
    
    [ObservableProperty]
    private int _proposedStructureNodesCount;

    [ObservableProperty]
    private Node _selectedNode;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isPreviewReady;

    [ObservableProperty]
    private string _statusMessage;

    private Services.FileOrganizer _fileOrganizer;
    private readonly string _targetBasePath;

    private readonly AnalysisViewModel _parent;

    public Step4ViewModel(AnalysisViewModel parent, IServiceProvider serviceProvider)
    {
        StepNumber = 4;
        Icon = "fa-brain";
        Description = "Step 4";
        _parent = parent;
        _targetBasePath = Path.Combine(
            Path.GetDirectoryName(parent.AnalysisPath),
            "organized_files"
        );
    }

    public void LoadFileList(List<FileItemViewModel> files)
    {
        AllFiles = new ObservableCollection<FileItemViewModel>(files);
        CurrentStructure = TreeViewHelper.BuildTree(AllFiles);
        CurrentStructureNodesCount = CurrentStructure.CountNodes();
        OnPropertyChanged(nameof(AllFiles));
        OnPropertyChanged(nameof(CurrentStructure));
    }

    public async Task PrepareOrganizeAsync(OrganizationType organizationType)
    {
        try
        {
            IsPreviewReady = false;
            StatusMessage = "Analyzing files...";

            var progress = new Progress<double>(p =>
            {
                Progress = p;
                StatusMessage = $"Analyzing files... {p:F1}%";
            }
            );

            _fileOrganizer = new Services.FileOrganizer(_parent.ChatCompletionService, _targetBasePath);

            var preview = await _fileOrganizer.PreviewOrganizationAsync(
                AllFiles, _parent.VectorCollection, _parent.Keys, progress);

            CurrentStructure = preview.CurrentStructure;
            ProposedStructure = preview.ProposedStructure;
            CurrentStructureNodesCount = CurrentStructure.CountNodes();
            ProposedStructureNodesCount = ProposedStructure.CountNodes();

            FileMoves = preview.FileMoves;

            StatusMessage = $"Preview ready. {FileMoves.Count} files will be organized.";
            IsPreviewReady = true;

            OnPropertyChanged(nameof(CurrentStructure));
            OnPropertyChanged(nameof(ProposedStructure));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error preparing preview: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExecuteOrganizationAsync()
    {
        try
        {
            StatusMessage = "Moving files...";
            var progress = new Progress<double>(p =>
                StatusMessage = $"Moving files... {p:F1}%");

            var results = await _fileOrganizer.ExecuteOrganizationAsync(
                FileMoves, progress);

            var succeeded = results.Count(r => r.Status == "Completed");
            var failed = results.Count(r => r.Status.StartsWith("Failed"));

            StatusMessage = $"Organization complete. {succeeded} files moved successfully, {failed} failed.";

            if (failed > 0)
            {
                // You might want to show failed moves in UI
                var failedMoves = results.Where(r => r.Status.StartsWith("Failed"));
                // Handle failed moves (perhaps show in a dialog)
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during organization: {ex.Message}";
        }
    }


    [RelayCommand]
    private void Previous()
    {
        _parent.PreviousStep();
    }
}