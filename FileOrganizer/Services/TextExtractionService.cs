using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using FileOrganizer.DataExtractor;
using FileOrganizer.ViewModels;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001

namespace FileOrganizer.Services;

public class TextExtractionService(ITextExtractorService extractorService) : ITextExtractionService
{
    private const int MaxConcurrentOperations = 10;

    public async Task ExtractAndEmbedAsync(
        IEnumerable<FileItemViewModel> files,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        List<string> keys,
        IProgress<(int Processed, int Total, string Message)> progress,
        CancellationToken cancellationToken = default)
    {
        var pdfFiles = new List<FileItemViewModel>();
        foreach (var file in files)
        {
            if (file.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                pdfFiles.Add(file);
            }
        }

        var totalFiles = pdfFiles.Count;
        var processedFiles = 0;
        var semaphore = new SemaphoreSlim(MaxConcurrentOperations);
        var tasks = new List<Task>();

        foreach (var pdfFile in pdfFiles)
        {
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await ProcessFileAsync(keys,pdfFile, vectorStoreCollection, textEmbeddingGenerationService);

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
                    pdfFile.Model.Content = ex.Message;
                    Interlocked.Increment(ref processedFiles);
                    progress.Report((processedFiles, totalFiles,
                        $"Error processing {pdfFile.Name}: {ex.Message}"));
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessFileAsync(List<string> keys,FileItemViewModel pdfFile,
        IVectorStoreRecordCollection<string, FileVectorStoreRecord> vectorStoreCollection,
        ITextEmbeddingGenerationService textEmbeddingGenerationService)
    {
        await using var fileStream = File.OpenRead(pdfFile.Model.Path);
        var extractedText = extractorService.ExtractTextFromPdf(
            pdfFile.Name, pdfFile.FileType, fileStream).Text;
        var splitInLines = TextChunker.SplitPlainTextLines(extractedText, 256);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(splitInLines, 512, 32);

        var titleEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(pdfFile.Name);
        pdfFile.Model.Content = extractedText;
        pdfFile.Model.HasContent = !string.IsNullOrEmpty(extractedText);
        if (pdfFile.Model.HasContent)
        {
            for (var index = 0; index < paragraphs.Count; index++)
            {
                var paragraph = paragraphs[index];
                var paragraphEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(paragraph);
                var combinedEmbedding = CombineEmbeddings(titleEmbedding, paragraphEmbedding, 0.5f, 0.5f);
                await vectorStoreCollection.UpsertAsync(new FileVectorStoreRecord()
                {
                    Description = pdfFile.Name,
                    Embedding = combinedEmbedding,
                    AdditionalMetadata = pdfFile.Model.Path,
                    Text = paragraph,
                    Id = $"{pdfFile.Model.Path}-${index}"
                });
                keys.Add($"{pdfFile.Model.Path}-${index}");
            }
        }
    }

    private float[] CombineEmbeddings(ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2, float weight1, float weight2)
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