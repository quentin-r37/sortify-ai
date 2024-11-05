using Microsoft.ML.Data;

namespace FileOrganizer.Models;

public class EmbeddingData
{
    [ColumnName("Features")]
    public float[] Features { get; set; }

    public string FilePath { get; set; }
}