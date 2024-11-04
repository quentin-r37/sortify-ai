using FileOrganizer.Models;
using FileOrganizer.UIService;
using FileOrganizer.ViewModels;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.Shared;
using Microsoft.Extensions.VectorData;

namespace FileOrganizer.Services;

public class FileOrganizer(IChatCompletionService chatService, string targetBasePath)
{
    private readonly CostEfficientFileOrganizer _organizationStrategy = new(chatService);

    public async Task<OrganizationPreview> PreviewOrganizationAsync(
        ObservableCollection<FileItemViewModel> files,
        IVectorStoreRecordCollection<string,FileVectorStoreRecord> vectorCollection,
        List<string> embeddingKeys,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        var processedFiles = await _organizationStrategy.OrganizeFilesAsync(files, vectorCollection, embeddingKeys, progress, cancellationToken);
        return EnhancedTreeViewHelper.BuildOrganizationPreview(files, processedFiles, targetBasePath);
    }

    public async Task<List<FileMove>> ExecuteOrganizationAsync(
        List<FileMove> moves,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FileMove>();
        var total = moves.Count;
        var processed = 0;

        foreach (var move in moves)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Create destination directory if it doesn't exist
                var destinationDir = Path.GetDirectoryName(move.DestinationPath);
                var directoryInfo = new DirectoryInfo(destinationDir);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
                // Move the file
                if (File.Exists(move.DestinationPath))
                {
                    // Handle conflicts based on your policy
                    // Here we're adding a number to the filename
                    var dir = Path.GetDirectoryName(move.DestinationPath);
                    var fileName = Path.GetFileNameWithoutExtension(move.DestinationPath);
                    var ext = Path.GetExtension(move.DestinationPath);
                    var counter = 1;

                    while (File.Exists(move.DestinationPath))
                    {
                        move.DestinationPath = Path.Combine(dir, $"{fileName}_{counter++}{ext}");
                    }
                }

                File.Move(move.SourcePath, move.DestinationPath);
                move.Status = "Completed";
            }
            catch (Exception ex)
            {
                move.Status = $"Failed: {ex.Message}";
            }

            results.Add(move);
            processed++;
            progress?.Report(processed / (double)total * 100);
        }

        return results;
    }
}