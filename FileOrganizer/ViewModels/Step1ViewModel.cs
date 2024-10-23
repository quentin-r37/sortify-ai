using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Services;
using FileOrganizer.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileOrganizer.ViewModels;

public partial class Step1ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    private readonly MainWindow _window;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _path;

    public Step1ViewModel(AnalysisViewModel parent, MainWindow window, IServiceProvider serviceProvider)
    {

        _parent = parent;
        _window = window;
        _serviceProvider = serviceProvider;
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
        var topLevel = TopLevel.GetTopLevel(_window);
        if (topLevel is not null)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder"
            });

            if (folders.Count > 0) Path = folders[0].Path.LocalPath;
        }
    }

    partial void OnPathChanged(string value)
    {
        _parent.AnalysisPath = value;
    }
}