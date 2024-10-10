namespace FileOrganizer.Shared;

public class Configuration
{
    public int Id { get; set; } // For SQLite database
    public DateTime CreatedDate { get; set; }
    public string Value { get; set; }
}