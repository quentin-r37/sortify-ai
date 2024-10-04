using System;

namespace FileOrganizer.Models;

public class FileItem
{
    public string Path { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string FileType { get; set; }
}