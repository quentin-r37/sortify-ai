using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Views;

namespace FileOrganizer.ViewModels;

public partial class Step1ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    private readonly MainWindow _window;

    public Step1ViewModel(AnalysisViewModel parent, MainWindow window, AppDbContext appDbContext)
    {
        _parent = parent;
        _window = window;
        StepNumber = 1;
        Icon = "fa-folder";
        Description = "Step 1";
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _path;

    private bool CanExecuteNext() => !string.IsNullOrEmpty(Path) && Directory.Exists(Path);

    [RelayCommand(CanExecute = nameof(CanExecuteNext))]
    private void Next()
    {
        _parent.Step2.LoadFiles(Path);
        _parent.NextStep();
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

            if (folders.Count > 0)
            {
                Path = folders[0].Path.LocalPath;
            }
        }
    }
    partial void OnPathChanged(string value) => _parent.AnalysisPath = value;

}