using System;
using Microsoft.Extensions.VectorData;

namespace FileOrganizer.Services;

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

    [VectorStoreRecordVector(384)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}