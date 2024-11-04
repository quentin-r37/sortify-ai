using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable SKEXP0001

namespace FileOrganizer.Services;

public interface ITextExtractionService
{
    Task ExtractAndEmbedAsync(
        int analysisId,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        List<string> keys,
        IProgress<(int Processed, int Total, string Message)> progress,
        IEnumerable<FileItemViewModel> files,
        CancellationToken cancellationToken = default);
}