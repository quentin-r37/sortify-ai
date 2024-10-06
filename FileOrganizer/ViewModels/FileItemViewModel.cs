using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FileOrganizer.Shared;

namespace FileOrganizer.ViewModels;

public class FileItemViewModel(FileItem model) : ObservableObject
{
    public FileItem Model { get; } = model;

    public string Name => Model.Name;
    public string DirectoryName => Model.DirectoryName;
    public long Size => Model.Size;
    public DateTime CreatedDate => Model.CreatedDate;
    public string FileType => Model.FileType;
}