using FileOrganizer.Models;
using FileOrganizer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FileOrganizer.UIService;

public abstract class EnhancedTreeViewHelper : TreeViewHelper
{
    public static OrganizationPreview BuildOrganizationPreview(
        ObservableCollection<FileItemViewModel> currentFiles,
        List<ProcessedFileData> processedFiles,
        string targetBasePath)
    {
        var preview = new OrganizationPreview
        {
            CurrentStructure = BuildTree(currentFiles),
            ProposedStructure = [],
            FileMoves = []
        };

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var processed in processedFiles)
        {
            var extension = Path.GetExtension(processed.FilePath);
            var destinationDir = Path.Combine(targetBasePath, processed.FolderName);
            var baseFileName = processed.FileName;
            var destinationFile = Path.Combine(destinationDir, baseFileName + extension);

            var counter = 1;
            while (seenPaths.Contains(destinationFile))
            {
                destinationFile = Path.Combine(destinationDir,
                    $"{baseFileName}_{counter++}{extension}");
            }
            seenPaths.Add(destinationFile);

            AddToTree(preview.ProposedStructure, destinationFile, targetBasePath);

            preview.FileMoves.Add(new FileMove
            {
                SourcePath = processed.FilePath,
                DestinationPath = destinationFile,
                WillOverwrite = File.Exists(destinationFile),
                Status = "Pending"
            });
        }

        return preview;
    }

    private static void AddToTree(ObservableCollection<Node> nodes, string filePath, string basePath)
    {
        var relativePath = Path.GetRelativePath(basePath, filePath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar);

        var current = nodes;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var isFile = i == parts.Length - 1;

            var existing = current.FirstOrDefault(n => n.Title == part);
            if (existing == null)
            {
                var newNode = new Node(part, isFile);
                current.Add(newNode);
                current = newNode.SubNodes;
            }
            else
            {
                current = existing.SubNodes;
            }
        }
    }
}