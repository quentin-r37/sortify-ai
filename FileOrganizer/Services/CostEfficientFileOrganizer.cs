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
using FileOrganizer.Shared;
using Microsoft.Extensions.VectorData;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FileOrganizer.Services;

public class CostEfficientFileOrganizer(IChatCompletionService chatService)
{
   private readonly Dictionary<string, string> _commonPatterns = new()
{
    // Financial documents
    { @"(?i)(invoice|receipt|budget|expense|financial|bank|statement|tax)", "Financial" },
    { @"(?i)(payroll|salary|invoice_\d+|quote|purchase[-_]order)", "Financial" },
    
    // Images and Photos
    { @"(?i)(IMG_|DCIM|photo|image|camera|screenshot)", "Photos" },
    { @"(?i)(PNG|JPG|JPEG|GIF|RAW|CR2|NEF|PSD|BMP)", "Photos" },
    { @"(?i)(wallpaper|background|album|gallery)", "Photos" },
    
    // Documents and Notes
    { @"(?i)(meeting|notes|memo|minutes|agenda)", "Meetings" },
    { @"(?i)(report|proposal|presentation|draft)", "Work" },
    { @"(?i)(manual|guide|instruction|tutorial)", "Documentation" },
    { @"(?i)(contract|agreement|legal|policy|terms)", "Legal" },
    { @"(?i)(resume|CV|cover[-_]letter|application)", "Career" },
    
    // Personal
    { @"(?i)(travel|vacation|trip|itinerary)", "Travel" },
    { @"(?i)(recipe|food|meal|cooking)", "Recipes" },
    { @"(?i)(medical|health|prescription|doctor)", "Health" },
    { @"(?i)(family|kids|baby|personal)", "Personal" },
    
    // Education
    { @"(?i)(homework|assignment|study|exam|course)", "Education" },
    { @"(?i)(thesis|dissertation|research|paper)", "Education" },
    
    // Media
    { @"(?i)(video|movie|film|MP4|AVI|MOV)", "Videos" },
    { @"(?i)(music|song|audio|MP3|WAV|playlist)", "Audio" },
    { @"(?i)(ebook|book|novel|pdf|epub)", "Books" },
    
    // Software and Development
    { @"(?i)(source|code|project|git|dev)", "Development" },
    { @"(?i)(backup|archive|copy|old|version)", "Backups" },
    { @"(?i)(config|setting|preference|setup)", "Settings" },
    
    // Common date patterns
    { @"(?i)(\d{4}[-_]\d{2}[-_]\d{2}|\d{2}[-_]\d{2}[-_]\d{4})", null },  // Will be used for dating
    { @"(?i)(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[-_]\d{4}", null }  // Month-year pattern
};

private readonly HashSet<string> _knownCategories = new(StringComparer.OrdinalIgnoreCase)
{
    "Financial", "Photos", "Documents", "Work", "Personal",
    "Travel", "Recipes", "Meetings", "Documentation", "Legal",
    "Career", "Health", "Education", "Videos", "Audio",
    "Books", "Development", "Backups", "Settings"
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
        IVectorStoreRecordCollection<string,FileVectorStoreRecord> vectorCollection,
        List<string> embeddingKeys,
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
            var groupedUnknown = (await ClusterSimilarFilesAsync(unknownFiles, vectorCollection, embeddingKeys, cancellationToken)).ToList();

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
                var secondPassProgress = (processedGroups / (double)totalGroups * 100 * (1 - firstPassWeight));
                progress?.Report((firstPassWeight * 100) + secondPassProgress);
            }
        }
        else
        {
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
    
  


    private async Task<IEnumerable<IGrouping<uint, FileItemViewModel>>> ClusterSimilarFilesAsync(
        List<FileItemViewModel> files,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorCollection,
        List<string> embeddingKeys,
        CancellationToken cancellationToken)
    {
        var vectorAsyncEnumerable =  vectorCollection.GetBatchAsync(embeddingKeys, cancellationToken: cancellationToken);
        var data = new List<EmbeddingData>();
        await foreach (var vectorFile in vectorAsyncEnumerable)
        {
            data.Add(new EmbeddingData()
            {
                FilePath = vectorFile.AdditionalMetadata,
                Features = vectorFile.Embedding.ToArray()
            });
        }
        
        var mlContext = new MLContext();
        var dataView = mlContext.Data.LoadFromEnumerable(data);
        int clusterCount = 6;
        var pipeline = mlContext.Clustering.Trainers.KMeans(numberOfClusters: clusterCount);
        var model = pipeline.Fit(dataView);
        var predictions = model.Transform(dataView);
        var predictedClusters = mlContext.Data.CreateEnumerable<ClusteringPrediction>(predictions, reuseRowObject: false).ToList();

        // Step 1: Associate embeddings with predicted clusters and files
        var embeddingClusters = data.Zip(predictedClusters, (embeddingData, prediction) => new
        {
            FilePath = embeddingData.FilePath,
            ClusterId = prediction.PredictedClusterId
        }).ToList();

        // Step 2: Group embeddings by FilePath
        var fileEmbeddings = embeddingClusters.GroupBy(ec => ec.FilePath);

        // Step 3: Determine the majority cluster for each file
        var fileClusterAssignments = fileEmbeddings.Select(group =>
        {
            var filePath = group.Key;
            var clusterCounts = group.GroupBy(ec => ec.ClusterId)
                .Select(g => new { ClusterId = g.Key, Count = g.Count() })
                .ToList();
            var assignedClusterId = clusterCounts.OrderByDescending(c => c.Count)
                .First().ClusterId;
            return new
            {
                FileItem = files.FirstOrDefault(f => f.Model.Path == filePath),
                ClusterId = assignedClusterId
            };
        }).Where(x => x.FileItem != null).ToList();

        // Step 4: Group files by their assigned clusters
        var clusteredFiles = fileClusterAssignments
            .GroupBy(x => x.ClusterId, x => x.FileItem)
            .ToList();

        return clusteredFiles;
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
            var content = GetFileSample(file.Model);

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

    private static string GetFileSample(FileItem file)
    {
        const int maxSampleSize = 1024;
        if (!string.IsNullOrEmpty(file.Content))
        {
            return file.Content.Length > maxSampleSize
                ? file.Content[..maxSampleSize]
                : file.Content;
        }
        return file.Path;
    }
}

public class EmbeddingData
{
    [ColumnName("Features")]
    [VectorType(384)]
    public float[] Features { get; set; }

    public string FilePath { get; set; }
}

public class ClusteringPrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedClusterId { get; set; }

    public float[] Score { get; set; }
}