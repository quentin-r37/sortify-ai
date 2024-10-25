using System.Collections.ObjectModel;

namespace FileOrganizer.Models;

public class Node(string title, bool isFile = false)
{
    public string Title { get; set; } = title;
    public ObservableCollection<Node> SubNodes { get; set; } = new ObservableCollection<Node>();
    public bool IsFile { get; set; } = isFile;
}