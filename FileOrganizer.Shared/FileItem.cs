namespace FileOrganizer.Shared;

public class FileItem
{
    public int FileItemId { get; set; }
    public int AnalysisId { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string FileType { get; set; }
    public string DirectoryName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool HasContent { get; set; }
    public Analysis? Analysis { get; set; }
    public List<string> DirectoryLevels { get; set; } = [];
}