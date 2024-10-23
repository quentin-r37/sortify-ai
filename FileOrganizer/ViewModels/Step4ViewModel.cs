using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FileOrganizer.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.ML.Data;
using LiveChartsCore;
using SkiaSharp;
using System.Linq;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace FileOrganizer.ViewModels;

public partial class Step4ViewModel : StepViewModel
{
    public ObservableCollection<FileItemViewModel> AllFiles { get; set; }
    private readonly AnalysisViewModel _parent;

    public Step4ViewModel(AnalysisViewModel parent, MainWindow window, IServiceProvider serviceProvider)
    {
        StepNumber = 4;
        Icon = "fa-brain";
        Description = "Step 4";
        _parent = parent;
    }

    public void LoadFileList(List<FileItemViewModel> files)
    {
        AllFiles = new ObservableCollection<FileItemViewModel>(files);
        OnPropertyChanged(nameof(AllFiles));
    }

    public async Task PreparePreviewAsync(OrganizationType organizationType)
    {
        // Retrieve the list of text files to process

        var textFiles = AllFiles;

        foreach (var file in textFiles)
        {
            var textContent = file.Content; // Assuming you have a way to get the text content
            var processedData = await ProcessSingleTextFileAsync(file.Model.Path, textContent);
            var test = processedData.Description;
            // Use processedData to organize files
            // For example, move or rename files based on processedData.FileName and processedData.FolderName
        }
    }

    public async Task<ProcessedFileData> ProcessSingleTextFileAsync(string filePath, string textContent)
    {
        // Define the prompts
        var summarizePrompt = $@"Provide a concise and accurate summary of the following text, focusing on the main ideas and key details.
Limit your summary to a maximum of 150 words.

Text: {textContent}

Summary:";

        var filenamePromptTemplate = @"Based on the summary below, generate a specific and descriptive filename that captures the essence of the document.
Limit the filename to a maximum of 3 words. Use nouns and avoid starting with verbs like 'depicts', 'shows', 'presents', etc.
Do not include any data type words like 'text', 'document', 'pdf', etc. Use only letters and connect words with underscores.

Summary: {0}

Examples:
1. Summary: A research paper on the fundamentals of string theory.
   Filename: fundamentals_of_string_theory

2. Summary: An article discussing the effects of climate change on polar bears.
   Filename: climate_change_polar_bears

Now generate the filename.

Output only the filename, without any additional text.

Filename:";

        var foldernamePromptTemplate = @"Based on the summary below, generate a general category or theme that best represents the main subject of this document.
This will be used as the folder name. Limit the category to a maximum of 2 words. Use nouns and avoid verbs.
Do not include specific details, words from the filename, or any generic terms like 'untitled' or 'unknown'.

Summary: {0}

Examples:
1. Summary: A research paper on the fundamentals of string theory.
   Category: physics

2. Summary: An article discussing the effects of climate change on polar bears.
   Category: environment

Now generate the category.

Output only the category, without any additional text.

Category:";

        // Generate the summary
        var summary = await GetCompletionAsync(summarizePrompt);

        // Generate the filename
        var filenamePrompt = string.Format(filenamePromptTemplate, summary);
        var filename = await GetCompletionAsync(filenamePrompt);

        // Generate the folder name
        var foldernamePrompt = string.Format(foldernamePromptTemplate, summary);
        var folderName = await GetCompletionAsync(foldernamePrompt);

        // Clean and process the outputs
        filename = CleanAIOutput(filename, maxWords: 3);
        if (string.IsNullOrEmpty(filename) || filename.ToLower() == "untitled")
        {
            filename = CleanAIOutput(summary, maxWords: 3);
        }
        if (string.IsNullOrEmpty(filename))
        {
            filename = "document_" + Path.GetFileNameWithoutExtension(filePath);
        }

        folderName = CleanAIOutput(folderName, maxWords: 2);
        if (string.IsNullOrEmpty(folderName) || folderName.ToLower() == "untitled")
        {
            folderName = "documents";
        }

        return new ProcessedFileData
        {
            FilePath = filePath,
            Description = summary,
            FileName = filename,
            FolderName = folderName
        };
    }

    private async Task<string> GetCompletionAsync(string prompt)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var messages = await _parent.ChatCompletionService.GetChatMessageContentsAsync(chatHistory);
        var completion = messages.FirstOrDefault()?.Content.Trim();
        return completion ?? string.Empty;
    }

    private string CleanAIOutput(string text, int maxWords)
    {
        text = Regex.Replace(text, @"[^\w\s]", " ");
        text = Regex.Replace(text, @"\d+", "");
        text = text.Trim();

        text = Regex.Replace(text, @"([a-z])([A-Z])", "$1 $2");

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => word.ToLower())
                        .Where(word => Regex.IsMatch(word, @"^[a-z]+$"));  // Keep only words with letters

        var unwantedWords = new HashSet<string> {
                "the", "and", "based", "generated", "this", "is", "filename", "file", "document", "text", "output", "only", "below", "category",
                "summary", "key", "details", "information", "note", "notes", "main", "ideas", "concepts", "in", "on", "of", "with", "by", "for",
                "to", "from", "a", "an", "as", "at", "i", "we", "you", "they", "he", "she", "it", "that", "which", "are", "were", "was", "be",
                "have", "has", "had", "do", "does", "did", "but", "if", "or", "because", "about", "into", "through", "during", "before", "after",
                "above", "below", "any", "each", "few", "more", "most", "other", "some", "such", "no", "nor", "not", "own", "same", "so",
                "than", "too", "very", "s", "t", "can", "will", "just", "don", "should", "now", "new", "depicts", "show", "shows", "display",
                "illustrates", "presents", "features", "provides", "covers", "includes", "discusses", "demonstrates", "describes", "depict",
                "show", "present", "feature", "provide", "cover", "include", "discuss", "demonstrate", "describe"
            };

        var stopWords = new HashSet<string> {
                "the", "is", "at", "which", "on", "and", "a"
            };

        var allUnwantedWords = unwantedWords.Union(stopWords).ToList();

        var filteredWords = new List<string>();
        var seenWords = new HashSet<string>();

        foreach (var word in words)
        {
            if (!allUnwantedWords.Contains(word) && !seenWords.Contains(word))
            {
                filteredWords.Add(word);
                seenWords.Add(word);
            }
        }
        filteredWords = filteredWords.Take(maxWords).ToList();
        return string.Join("_", filteredWords);
    }
}


public class ProcessedFileData
{
    public string FilePath { get; set; }
    public string FolderName { get; set; }
    public string FileName { get; set; }
    public string Description { get; set; }
}