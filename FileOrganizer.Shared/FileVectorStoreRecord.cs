using Microsoft.Extensions.VectorData;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileOrganizer.Shared;

public class FileVectorStoreRecord
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordData]
    public string Text { get; set; }

    [VectorStoreRecordData]
    public bool IsReference { get; set; }

    [VectorStoreRecordData]
    public string ExternalSourceName { get; set; }

    [VectorStoreRecordData]
    public string AdditionalMetadata { get; set; }

    [NotMapped]
    [VectorStoreRecordVector(384)]
    public ReadOnlyMemory<float> Embedding { get; set; }

    public int? AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public string? EmbeddingAsString { get; set; }

}