using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using FileOrganizer.Database;
using FileOrganizer.DataExtractor;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001

namespace FileOrganizer.Services;

public class TextExtractionService(IDocumentTextExtractor documentTextExtractor, AppDbContext dbContext) : ITextExtractionService
{
    private const int MaxConcurrentOperations = 10;

    public async Task ExtractAndEmbedAsync(
        int analysisId,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        List<string> keys,
        IProgress<(int Processed, int Total, string Message)> progress,
        IEnumerable<FileItemViewModel> files,
        CancellationToken cancellationToken = default)
    {
        var documentFiles = files.WhereFileTypeIsDocument();
        var totalFiles = documentFiles.Count;
        var processedFiles = 0;

        var processedFilePaths = await RestoreExistingVectorsAsync(analysisId, vectorStoreCollection, keys, progress, totalFiles);
        if (processedFilePaths.Any())
            processedFiles = documentFiles.Count(f => processedFilePaths.Contains(f.Model.Path));

        var remainingFiles = documentFiles.Where(f => !processedFilePaths.Contains(f.Model.Path)).ToList();
        if (!remainingFiles.Any())
            return;

        var semaphore = new SemaphoreSlim(MaxConcurrentOperations);
        var tasks = new List<Task>();

        foreach (var document in remainingFiles)
        {
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await ProcessFileAsync(analysisId, keys, document, vectorStoreCollection, textEmbeddingGenerationService);

                    Interlocked.Increment(ref processedFiles);
                    progress.Report((processedFiles, totalFiles,
                        $"Processed and embedded {processedFiles} of {totalFiles} files"));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    document.Model.Content = ex.Message;
                    Interlocked.Increment(ref processedFiles);
                    progress.Report((processedFiles, totalFiles,
                        $"Error processing {document.Name}: {ex.Message}"));
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessFileAsync(int analysisId, List<string> keys, FileItemViewModel file,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        ITextEmbeddingGenerationService textEmbeddingGenerationService)
    {
        await using var dbContext = new AppDbContext(new DbContextOptions<AppDbContext>());
        var analysis = await dbContext.Analyses.FindAsync(analysisId);
        await using var fileStream = File.OpenRead(file.Model.Path);
        var extractedText = documentTextExtractor.ExtractTextAsync(
            file.Name, file.FileType, fileStream).Text;
        var splitInLines = TextChunker.SplitPlainTextLines(extractedText, 256);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(splitInLines, 512, 32);

        var titleEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(file.Name);
        file.Model.Content = extractedText;
        file.Model.HasContent = !string.IsNullOrEmpty(extractedText);
        if (file.Model.HasContent)
        {
            for (var index = 0; index < paragraphs.Count; index++)
            {
                var paragraph = paragraphs[index];
                var paragraphEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(paragraph);
                var combinedEmbedding = CombineEmbeddings(titleEmbedding, paragraphEmbedding, 0.4f, 0.6f);

                var fileVector = new FileVectorStoreRecord()
                {
                    Description = file.Name,
                    Embedding = combinedEmbedding,
                    AdditionalMetadata = file.Model.Path,
                    Text = paragraph,
                    Id = $"{file.Model.Path}-${index}",
                    EmbeddingAsString = JsonSerializer.Serialize(combinedEmbedding),
                    ExternalSourceName = string.Empty,
                };
                await vectorStoreCollection.UpsertAsync(fileVector);
                if (analysis != null)
                {
                    analysis.Vectors ??= [];
                    analysis.Vectors.Add(fileVector);
                }

                keys.Add($"{file.Model.Path}-${index}");
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<HashSet<string>> RestoreExistingVectorsAsync(
        int analysisId,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        List<string> keys,
        IProgress<(int Processed, int Total, string Message)> progress,
        int totalFiles)
    {
        var processedFilePaths = new HashSet<string>();

        await using var dbContext = new AppDbContext(new DbContextOptions<AppDbContext>());
        var analysis = await dbContext.Analyses
            .Include(a => a.Vectors)
            .FirstOrDefaultAsync(a => a.Id == analysisId);

        if (analysis?.Vectors != null && analysis.Vectors.Any())
        {
            foreach (var vector in analysis.Vectors.Where(vector => !string.IsNullOrEmpty(vector.EmbeddingAsString)))
            {
                vector.Embedding = JsonSerializer.Deserialize<float[]>(vector.EmbeddingAsString);
                await vectorStoreCollection.UpsertAsync(vector);
                keys.Add(vector.Id);
                processedFilePaths.Add(vector.AdditionalMetadata);
            }
            var alreadyProcessedCount = processedFilePaths.Count;
            progress.Report((alreadyProcessedCount, totalFiles,
                $"Restored {alreadyProcessedCount} previously processed files"));
        }

        return processedFilePaths;
    }


    private static float[] CombineEmbeddings(ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2, float weight1, float weight2)
    {
        if (embedding1.Length != embedding2.Length)
        {
            throw new ArgumentException("Embeddings must have the same length");
        }
        var combinedEmbedding = new float[embedding1.Length];
        var span1 = embedding1.Span;
        var span2 = embedding2.Span;
        for (var i = 0; i < span1.Length; i++)
        {
            combinedEmbedding[i] = (span1[i] * weight1) + (span2[i] * weight2);
        }

        return combinedEmbedding;
    }


}