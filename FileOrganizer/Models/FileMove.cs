namespace FileOrganizer.Models;

public class FileMove
{
    public string SourcePath { get; set; }
    public string DestinationPath { get; set; }
    public bool WillOverwrite { get; set; }
    public string Status { get; set; }
}