using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileOrganizer.DataExtractor;
using FileOrganizer.ViewModels;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001

namespace FileOrganizer.Services;

public class TextExtractionService(ITextExtractorService extractorService) : ITextExtractionService
{
    private const int MaxConcurrentOperations = 2;

    public async Task ExtractAndEmbedAsync(
        IEnumerable<FileItemViewModel> files,
        SemanticTextMemory memory,
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

                    await ProcessFileAsync(pdfFile, memory);

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

    private async Task ProcessFileAsync(FileItemViewModel pdfFile, SemanticTextMemory memory)
    {
        await using var fileStream = File.OpenRead(pdfFile.Model.Path);
        var extractedText = extractorService.ExtractTextFromPdf(
            pdfFile.Name, pdfFile.FileType, fileStream).Text;
        var splitInLines = TextChunker.SplitPlainTextLines(extractedText, 256);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(splitInLines, 512, 32);

        pdfFile.Model.HasContent = !string.IsNullOrEmpty(extractedText);
        if (pdfFile.Model.HasContent)
        {
            for (var index = 0; index < paragraphs.Count; index++)
            {
                var paragraph = paragraphs[index];
                await memory.SaveInformationAsync(
                    collection: "pdf_documents",
                    id: $"{pdfFile.Model.Path}-${index}",
                    additionalMetadata: pdfFile.Model.Path,
                    text: paragraph,
                    description: pdfFile.Name);
            }
        }
    }
}