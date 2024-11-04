using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.UIService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileOrganizer.ViewModels;

public partial class Step1ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFolderPickerService _folderPickerService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _path;

    public Step1ViewModel(AnalysisViewModel parent, IServiceProvider serviceProvider)
    {

        _parent = parent;
        _serviceProvider = serviceProvider;
        _folderPickerService = _serviceProvider.GetRequiredService<IFolderPickerService>();
        StepNumber = 1;
        Icon = "fa-folder";
        Description = "Step 1";
    }

    private bool CanExecuteNext()
    {
        return !string.IsNullOrEmpty(Path) && Directory.Exists(Path);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteNext))]
    private void Next()
    {
        _parent.Step2.LoadFiles(Path);
        _parent.NextStep();
        Task.Factory.StartNew(
            async () => await _parent.Step2.ExtractTextAndGenerateEmbeddingsAsync().ConfigureAwait(false),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    }

    [RelayCommand]
    private async Task Browse()
    {

        var folders = await _folderPickerService.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder"
        });
        if (folders.Count > 0) Path = folders[0].Path.LocalPath;
    }

    partial void OnPathChanged(string value)
    {
        _parent.AnalysisPath = value;
        _ = SavePathAsync(_parent);

    }

    public async Task SavePathAsync(AnalysisViewModel analysis)
    {
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        var entry = dbContext.Analyses.Entry(analysis.Model);
        entry.Entity.Path = analysis.AnalysisPath;
        await dbContext.SaveChangesAsync();
    }


}