using FileOrganizer.Models;
using FileOrganizer.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FileOrganizer.UIService;

public class TreeViewHelper
{
    public static ObservableCollection<Node> BuildTree(ObservableCollection<FileItemViewModel> files)
    {
        var rootNodes = new ObservableCollection<Node>();
        var groupedFiles = files.GroupBy(f => f.DirectoryLevels.FirstOrDefault() ?? "");
        foreach (var group in groupedFiles.OrderBy(g => g.Key))
        {
            if (string.IsNullOrEmpty(group.Key))
            {
                foreach (var file in group.OrderBy(f => f.Name))
                {
                    rootNodes.Add(new Node(file.Name, isFile: true));
                }
            }
            else
            {
                var currentNode = new Node(group.Key);
                rootNodes.Add(currentNode);
                AddFilesToNode(currentNode, group, 1);
            }
        }
        return rootNodes;
    }

    private static void AddFilesToNode(Node parentNode, IEnumerable<FileItemViewModel> files, int currentLevel)
    {
        var groupedItems = files
            .GroupBy(f => f.DirectoryLevels.Count > currentLevel ? f.DirectoryLevels[currentLevel] : null)
            .OrderBy(g => g.Key);

        foreach (var group in groupedItems)
        {
            if (group.Key == null)
            {
                foreach (var file in group.OrderBy(f => f.Name))
                {
                    parentNode.SubNodes.Add(new Node(file.Name, isFile: true));
                }
            }
            else
            {
                var newNode = new Node(group.Key);
                parentNode.SubNodes.Add(newNode);
                AddFilesToNode(newNode, group, currentLevel + 1);
            }
        }
    }
}