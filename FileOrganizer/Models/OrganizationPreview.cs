using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FileOrganizer.Models;

public class OrganizationPreview
{
    public ObservableCollection<Node> CurrentStructure { get; set; }
    public ObservableCollection<Node> ProposedStructure { get; set; }
    public List<FileMove> FileMoves { get; set; }
}