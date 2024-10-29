using System;
using System.Collections.ObjectModel;

namespace FileOrganizer.Models;

public class Node(string title, bool isFile = false)
{
    public string Title { get; set; } = title;
    public ObservableCollection<Node> SubNodes { get; set; } = [];
    public bool IsFile { get; set; } = isFile;

    public override bool Equals(object obj)
    {
        return obj is Node node && Equals(node);
    }

    protected bool Equals(Node other)
    {
        return Title == other.Title && IsFile == other.IsFile;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Title, IsFile);
    }
}