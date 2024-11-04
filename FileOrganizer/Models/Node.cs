using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FileOrganizer.Models;

public class Node(string title, bool isFile = false)
{
    public string Title { get; set; } = title;
    public ObservableCollection<Node> SubNodes { get; set; } = [];
    private bool IsFile { get; set; } = isFile;

    public override bool Equals(object obj)
    {
        return obj is Node node && Equals(node);
    }

    private bool Equals(Node other)
    {
        return Title == other.Title && IsFile == other.IsFile;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Title, IsFile);
    }
}

public static class NodeExtensions
{
    public static int CountNodes(this IEnumerable<Node> nodes)
    {
        if (nodes == null) return 0;
        var enumerable = nodes.ToList();
        return enumerable.Count + enumerable.Sum(node => CountNodes(node.SubNodes));
    }
    
}