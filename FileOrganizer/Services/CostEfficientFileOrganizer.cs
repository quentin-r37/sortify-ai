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
        "Travel", "Recipes", "Meetings", "Documentation", "Archive", "Miscellaneous"
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
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorCollection,
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
                        FileName = CleanFileName(file.Name),
                        Description = aiProcessed.Description
                    }));
                }
                else
                {
                    results.AddRange(group.Select(file => new ProcessedFileData
                    {
                        FilePath = file.Model.Path,
                        FolderName = "Miscellaneous",
                        FileName = CleanFileName(file.Name),
                        Description = "Miscellaneous file"
                    }));

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
                FileName = CleanFileName(file.Name),
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
        // Load embeddings
        var vectorAsyncEnumerable = vectorCollection.GetBatchAsync(embeddingKeys, cancellationToken: cancellationToken);
        var data = new List<EmbeddingData>();
        await foreach (var vectorFile in vectorAsyncEnumerable)
        {
            data.Add(new EmbeddingData()
            {
                FilePath = vectorFile.AdditionalMetadata,
                Features = vectorFile.Embedding.ToArray()
            });
        }

        int embeddingSize = data.FirstOrDefault()?.Features.Length ?? 0;
        if (embeddingSize == 0) throw new InvalidOperationException("No embeddings found.");

        var mlContext = new MLContext();

        var schemaDefinition = SchemaDefinition.Create(typeof(EmbeddingData));
        schemaDefinition["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, embeddingSize);
        var dataView = mlContext.Data.LoadFromEnumerable(data, schemaDefinition);

        // Determine optimal number of clusters
        var optimalClusters = DetermineOptimalClusters(mlContext, dataView, minClusters: 5, maxClusters: 15);

        // Train model with optimal clusters
        var pipeline = mlContext.Clustering.Trainers.KMeans(numberOfClusters: optimalClusters);
        var model = pipeline.Fit(dataView);
        var predictions = model.Transform(dataView);
        var predictedClusters = mlContext.Data.CreateEnumerable<ClusteringPrediction>(predictions, reuseRowObject: false).ToList();

        // Associate embeddings with predicted clusters
        var embeddingClusters = data.Zip(predictedClusters, (embeddingData, prediction) => new
        {
            FilePath = embeddingData.FilePath,
            ClusterId = prediction.PredictedClusterId
        }).ToList();

        var fileEmbeddings = embeddingClusters.GroupBy(ec => ec.FilePath);

        // Determine majority cluster for each file
        var fileClusterAssignments = fileEmbeddings.Select(group =>
        {
            var filePath = group.Key;
            var majorityCluster = group.GroupBy(ec => ec.ClusterId)
                .OrderByDescending(g => g.Count())
                .First().Key;
            return new
            {
                FileItem = files.FirstOrDefault(f => f.Model.Path == filePath),
                ClusterId = majorityCluster
            };
        }).Where(x => x.FileItem != null).ToList();

        return fileClusterAssignments.GroupBy(x => x.ClusterId, x => x.FileItem);
    }

    private int DetermineOptimalClusters(MLContext mlContext, IDataView data, int minClusters = 5, int maxClusters = 15)
    {
        var inertias = new List<double>();

        for (int k = minClusters; k <= maxClusters; k++)
        {
            var pipeline = mlContext.Clustering.Trainers.KMeans(numberOfClusters: k);
            var model = pipeline.Fit(data);
            var predictions = model.Transform(data);

            var inertia = CalculateInertia(mlContext, predictions);
            inertias.Add(inertia);
        }

        var optimalK = FindElbowPoint(inertias, minClusters);

        return optimalK;
    }

    private double CalculateInertia(MLContext mlContext, IDataView predictions)
    {
        var predictionData = mlContext.Data.CreateEnumerable<ClusteringPrediction>(
            predictions, reuseRowObject: false).ToList();

        return predictionData.Sum(p => p.Score.Sum(s => s * s));
    }

    private int FindElbowPoint(List<double> inertias, int minClusters)
    {
        var secondDerivatives = new List<double>();
        for (int i = 1; i < inertias.Count - 1; i++)
        {
            var secondDerivative = inertias[i - 1] - 2 * inertias[i] + inertias[i + 1];
            secondDerivatives.Add(secondDerivative);
        }

        var maxCurvatureIndex = secondDerivatives.IndexOf(secondDerivatives.Max());

        return maxCurvatureIndex + minClusters + 1;
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

    private static string CleanFileName(string fileName)
    {
        var cleaned = Path.GetFileNameWithoutExtension(fileName);
        return cleaned;
    }

    private async Task<ProcessedFileData> ProcessWithAIAsync(
        FileItemViewModel file,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = GetFileSample(file.Model);

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage($"""
                                        Categorize this file based on its name and sample content. 
                                        Choose one category from this list: {string.Join(", ", _knownCategories)}
                                        Reply with just the category name, nothing else.
                                        
                                        ---
                                        
                                        Filename: {Path.GetFileName(file.Model.Path)}
                                        
                                        Content sample: {content}
                                        
                                        ---

                                        The category :
                                        """);

            var messages = await chatService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);
            var s = messages.FirstOrDefault()?.Content;
            Debug.WriteLine("Category:"+s);
            Debug.WriteLine("Content:" + content);

            if (s != null)
            {
                var category = s.Trim();
                if (!string.IsNullOrEmpty(category) && _knownCategories.Contains(category))
                {
                    return new ProcessedFileData
                    {
                        FilePath = file.Model.Path,
                        FolderName = category,
                        FileName = CleanFileName(file.Name),
                        Description = "Categorized using AI assistance"
                    };
                }
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