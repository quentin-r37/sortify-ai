using System;
using System.Collections.Generic;
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
    public string Content => Model.Content;
    public bool HasContent => Model.HasContent;
    public double Relevance { get; set; }
    public List<string> DirectoryLevels { get; set; } = [];

}