using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.ViewModels;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
#pragma warning disable SKEXP0001

namespace FileOrganizer.Services;

public interface ITextExtractionService
{
    Task ExtractAndEmbedAsync(
        IEnumerable<FileItemViewModel> files,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        IProgress<(int Processed, int Total, string Message)> progress,
        CancellationToken cancellationToken = default);
}