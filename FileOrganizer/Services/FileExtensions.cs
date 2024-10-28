using FileOrganizer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileOrganizer.Services;

public static class FileExtensions
{
    public static readonly List<string> DocumentExtensions = [".pdf", ".txt"];
    public static List<T> WhereFileTypeIsDocument<T>(this IEnumerable<T> source)
        where T : FileItemViewModel
    {
        return source.Where(model => DocumentExtensions.Contains(model.FileType, StringComparer.OrdinalIgnoreCase)).ToList();
    }
}