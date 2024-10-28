using FileOrganizer.Models;
using FileOrganizer.ViewModels;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileOrganizer.Services;

public class CostEfficientFileOrganizer(IChatCompletionService chatService)
{
    private readonly Dictionary<string, string> _commonPatterns = new()
    {
        // Financial documents
        { @"(?i)(invoice|receipt|budget|expense|financial|bank|statement|tax)", "Financial" },
            
        // Images and Photos
        { @"(?i)(IMG_|DCIM|photo|image|camera|screenshot)", "Photos" },
            
        // Documents and Notes
        { @"(?i)(meeting|notes|memo|minutes|agenda)", "Meetings" },
        { @"(?i)(report|proposal|presentation|draft)", "Work" },
        { @"(?i)(manual|guide|instruction|tutorial)", "Documentation" },
            
        // Personal
        { @"(?i)(travel|vacation|trip|itinerary)", "Travel" },
        { @"(?i)(recipe|food|meal|cooking)", "Recipes" },
            
        // Common date patterns
        { @"(?i)(\d{4}[-_]\d{2}[-_]\d{2}|\d{2}[-_]\d{2}[-_]\d{4})", null }  // Will be used for dating
    };
    private readonly HashSet<string> _knownCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Financial", "Photos", "Documents", "Work", "Personal",
        "Travel", "Recipes", "Meetings", "Documentation"
    };

    // Pre-defined patterns for common file types
    // Financial documents
    // Images and Photos
    // Documents and Notes
    // Personal
    // Common date patterns
    // Will be used for dating

    public async Task<List<ProcessedFileData>> OrganizeFilesAsync(
        IEnumerable<FileItemViewModel> files,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessedFileData>();
        var unknownFiles = new List<FileItemViewModel>();
        var fileItemViewModels = files.ToList();
        var totalFiles = fileItemViewModels.Count;
        var processed = 0;

        const double firstPassWeight = 0.1;

        // First pass: Use pattern matching and heuristics
        foreach (var file in fileItemViewModels)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var processedData = TryProcessFileWithHeuristics(file);

            if (processedData != null)
            {
                results.Add(processedData);
                LogToDebug($"Successfully categorized: {file.Model.Path} -> {processedData.FolderName} (using heuristics)");
            }
            else
            {
                unknownFiles.Add(file);
                LogToDebug($"Heuristics failed for: {file.Model.Path}");
            }

            processed++;
            progress?.Report(processed / (double)totalFiles * 100 * firstPassWeight);
        }

        if (unknownFiles.Any())
        {
            var groupedUnknown = GroupSimilarFiles(unknownFiles).ToList();
            var totalGroups = groupedUnknown.Count;
            var processedGroups = 0;

            foreach (var group in groupedUnknown)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var representative = group.First();
                var aiProcessed = await ProcessWithAIAsync(representative, cancellationToken);

                if (aiProcessed != null)
                {
                    results.AddRange(group.Select(file => new ProcessedFileData
                    {
                        FilePath = file.Model.Path,
                        FolderName = aiProcessed.FolderName,
                        FileName = GenerateUniqueFileName(file, aiProcessed.FolderName),
                        Description = aiProcessed.Description
                    }));
                }
                else
                {
                    LogToDebug($"AI categorization failed for group represented by: {representative.Model.Path}");
                    foreach (var file in group)
                    {
                        LogToDebug($"  - Uncategorized file: {file.Model.Path}");
                    }
                }

                processedGroups++;
                // Second pass uses remaining 50% of the progress bar
                var secondPassProgress = (processedGroups / (double)totalGroups * 100 * (1 - firstPassWeight));
                progress?.Report((firstPassWeight * 100) + secondPassProgress);
            }
        }
        else
        {
            // If no AI processing was needed, complete the progress bar
            progress?.Report(100);
        }

        return results;
    }

    private ProcessedFileData TryProcessFileWithHeuristics(FileItemViewModel file)
    {
        var fileName = Path.GetFileNameWithoutExtension(file.Model.Path);
        var extension = Path.GetExtension(file.Model.Path).ToLowerInvariant();

        var category = GetCategoryFromExtension(extension);

        if (string.IsNullOrEmpty(category))
        {
            foreach (var pattern in _commonPatterns.Where(pattern => Regex.IsMatch(fileName, pattern.Key)))
            {
                category = pattern.Value;
                break;
            }
        }

        if (!string.IsNullOrEmpty(category))
        {
            return new ProcessedFileData
            {
                FilePath = file.Model.Path,
                FolderName = category,
                FileName = GenerateUniqueFileName(file, category),
                Description = $"Automatically categorized based on {(category == GetCategoryFromExtension(extension) ? "file type" : "filename pattern")}"
            };
        }

        return null;
    }

    private static string GetCategoryFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" => "Photos",
            ".mp3" or ".wav" => "Sounds",
            _ => string.Empty
        };
    }

    private IEnumerable<IGrouping<string, FileItemViewModel>> GroupSimilarFiles(List<FileItemViewModel> files)
    {
        return files.GroupBy(GetFileSignature);
    }

    private static string GetFileSignature(FileItemViewModel file)
    {
        var fileName = Path.GetFileNameWithoutExtension(file.Model.Path);
        var extension = Path.GetExtension(file.Model.Path);

        var pattern = Regex.Replace(fileName, @"[0-9]+", "N");
        pattern = Regex.Replace(pattern, @"[a-zA-Z]+", "L");
        var lengthCategory = fileName.Length switch
        {
            < 10 => "S",
            < 20 => "M",
            _ => "L"
        };

        return $"{extension}_{pattern}_{lengthCategory}";
    }

    private static void LogToDebug(string message)
    {
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        Console.WriteLine(logEntry);
    }

    private string GenerateUniqueFileName(FileItemViewModel file, string category)
    {
        var fileName = Path.GetFileNameWithoutExtension(file.Model.Path);
        var cleaned = CleanFileName(fileName);

        var dateMatch = Regex.Match(fileName, @"(\d{4}[-_]\d{2}[-_]\d{2}|\d{2}[-_]\d{2}[-_]\d{4})");
        if (dateMatch.Success)
        {
            cleaned = $"{cleaned}_{dateMatch.Value}";
        }

        return cleaned;
    }

    private static string CleanFileName(string fileName)
    {
        var cleaned = Regex.Replace(fileName, @"[^\w\s-]", "");
        cleaned = Regex.Replace(cleaned, @"[-\s]+", "_");
        return cleaned.Trim('_');
    }

    private async Task<ProcessedFileData> ProcessWithAIAsync(
        FileItemViewModel file,
        CancellationToken cancellationToken)
    {
        try
        {
            // Only read a sample of the file content for large files
            var content = await GetFileSampleAsync(file.Model.Path);

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage($@"Categorize this file based on its name and sample content. 
Filename: {Path.GetFileName(file.Model.Path)}
Content sample: {content}

Choose one category from this list: {string.Join(", ", _knownCategories)}
Reply with just the category name, nothing else.");

            var messages = await chatService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);
            var category = messages.FirstOrDefault()?.Content.Trim();

            if (!string.IsNullOrEmpty(category) && _knownCategories.Contains(category))
            {
                return new ProcessedFileData
                {
                    FilePath = file.Model.Path,
                    FolderName = category,
                    FileName = GenerateUniqueFileName(file, category),
                    Description = "Categorized using AI assistance"
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AI processing failed for {file.Model.Path}: {ex.Message}");
        }

        return null;
    }

    private static async Task<string> GetFileSampleAsync(string filePath)
    {
        const int maxSampleSize = 1024;
        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);

            var sample = new char[maxSampleSize];
            var read = await reader.ReadBlockAsync(sample, 0, maxSampleSize);

            return new string(sample, 0, read);
        }
        catch
        {
            return Path.GetFileName(filePath);
        }
    }
}