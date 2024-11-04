using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FileOrganizer.ViewModels;

public partial class FileItemViewModel(FileItem model) : ViewModelBase
{
    public FileItem Model { get; } = model;
    public string Name => Model.Name;
    public string DirectoryName => Model.DirectoryName;
    public long Size => Model.Size;
    public DateTime CreatedDate => Model.CreatedDate;
    public string FileType => Model.FileType;
    public string Content => Model.Content;
    public bool HasContent => Model.HasContent;
    public double Relevance { get; set; }
    public List<string> DirectoryLevels => Model.DirectoryLevels;

    [RelayCommand]
    private void OpenFile()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Model.Path,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", Model.Path);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", Model.Path);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening file: {ex.Message}");
        }
    }

}