namespace FileOrganizer.Shared
{
    public class Analysis
    {
        public int Id { get; set; } // For SQLite database
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreatedDate { get; set; }
        public OrganizationType OrganizationType { get; set; }
        public List<FileItem> Files { get; set; }
        public List<FileVectorStoreRecord> Vectors { get; set; }

    }
}
