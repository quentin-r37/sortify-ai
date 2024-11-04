using FileOrganizer.Database;
using FileOrganizer.DataExtractor;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

        var (processedFilePaths, restoredCount) = await RestoreExistingVectorsAsync(
            analysisId,
            vectorStoreCollection,
            keys,
            progress,
            totalFiles,
            documentFiles,
            cancellationToken);

        processedFiles = restoredCount;

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
                    document.Model.HasContent = false;
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

        // Extract text content
        var extractedText = await ExtractTextFromFileAsync(file);

        // Update file model
        file.Model.Content = extractedText;
        file.Model.HasContent = !string.IsNullOrEmpty(extractedText);

        if (file.Model.HasContent)
        {
            var splitInLines = TextChunker.SplitPlainTextLines(extractedText, 256);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(splitInLines, 512, 32);

            var titleEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(file.Name);

            for (var index = 0; index < paragraphs.Count; index++)
            {
                var key = $"{analysisId}-{file.Model.Path}-${index}";
                var paragraph = paragraphs[index];
                var paragraphEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(paragraph);
                var combinedEmbedding = CombineEmbeddings(titleEmbedding, paragraphEmbedding, 0.4f, 0.6f);

                var fileVector = new FileVectorStoreRecord()
                {
                    Description = file.Name,
                    Embedding = combinedEmbedding,
                    AdditionalMetadata = file.Model.Path,
                    Text = paragraph,
                    Id = key,
                    EmbeddingAsString = JsonSerializer.Serialize(combinedEmbedding),
                    ExternalSourceName = string.Empty,
                };
                await vectorStoreCollection.UpsertAsync(fileVector);
                if (analysis != null)
                {
                    analysis.Vectors ??= [];
                    analysis.Vectors.Add(fileVector);
                }

                keys.Add(key);
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<string> ExtractTextFromFileAsync(FileItemViewModel file)
    {
        await using var fileStream = File.OpenRead(file.Model.Path);
        return documentTextExtractor.ExtractTextAsync(file.Name, file.FileType, fileStream).Text;
    }

    private async Task<(HashSet<string> ProcessedPaths, int ProcessedCount)> RestoreExistingVectorsAsync(
        int analysisId,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        List<string> keys,
        IProgress<(int Processed, int Total, string Message)> progress,
        int totalFiles,
        IEnumerable<FileItemViewModel> files,
        CancellationToken cancellationToken = default)
    {
        var processedFilePaths = new HashSet<string>();
        var processedCount = 0;

        await using var dbContext = new AppDbContext(new DbContextOptions<AppDbContext>());
        var analysis = await dbContext.Analyses
            .Include(a => a.Vectors)
            .FirstOrDefaultAsync(a => a.Id == analysisId, cancellationToken: cancellationToken);

        if (analysis?.Vectors != null && analysis.Vectors.Any())
        {
            var filePathsToProcess = analysis.Vectors
                .Select(v => v.AdditionalMetadata)
                .Distinct()
                .ToList();

            var semaphore = new SemaphoreSlim(MaxConcurrentOperations);
            var tasks = new List<Task>();

            foreach (var filePath in filePathsToProcess)
            {
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var file = files.FirstOrDefault(f => f.Model.Path == filePath);
                        if (file != null)
                        {
                            // Extract text content for restored files
                            var extractedText = await ExtractTextFromFileAsync(file);
                            file.Model.Content = extractedText;
                            file.Model.HasContent = !string.IsNullOrEmpty(extractedText);

                            var currentCount = Interlocked.Increment(ref processedCount);
                            progress.Report((currentCount, totalFiles,
                                $"Restored and extracted content from {currentCount} of {totalFiles} files"));
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            // Now restore vectors
            foreach (var vector in analysis.Vectors.Where(vector => !string.IsNullOrEmpty(vector.EmbeddingAsString)))
            {
                if (vector.EmbeddingAsString != null)
                    vector.Embedding = JsonSerializer.Deserialize<float[]>(vector.EmbeddingAsString);
                await vectorStoreCollection.UpsertAsync(vector, cancellationToken: cancellationToken);
                keys.Add(vector.Id);
                processedFilePaths.Add(vector.AdditionalMetadata);
            }

            progress.Report((processedCount, totalFiles,
                $"Completed vector restoration for {processedCount} files"));
        }

        return (processedFilePaths, processedCount);
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