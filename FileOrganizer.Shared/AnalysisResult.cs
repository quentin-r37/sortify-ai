namespace FileOrganizer.Shared;

public class AnalysisResult
{
    public Dictionary<string, long> SizeByType { get; set; }
    public Dictionary<string, long> SizeByDate { get; set; }
    public Dictionary<DateTime, int> CountByDate { get; set; }
    public Dictionary<DateTime, int> CountByType { get; set; }

}