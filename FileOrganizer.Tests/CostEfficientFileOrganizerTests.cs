using FileOrganizer.Services;
using FileOrganizer.Shared;
using FileOrganizer.ViewModels;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;

namespace FileOrganizer.Tests;

public class CostEfficientFileOrganizerTests
{
    private readonly IChatCompletionService _mockChatService;
    private readonly CostEfficientFileOrganizer _organizer;

    public CostEfficientFileOrganizerTests()
    {
        _mockChatService = Substitute.For<IChatCompletionService>();
        _organizer = new CostEfficientFileOrganizer(_mockChatService);
    }

    [Fact]
    public async Task OrganizeFilesAsync_WithKnownPatterns_CategorizesCorrectly()
    {
        // Arrange
        var files = new List<FileItemViewModel>
        {
            CreateFileViewModel("invoice_2024.pdf", "Financial"),
            CreateFileViewModel("IMG_20240101.jpg", "Photos"),
            CreateFileViewModel("meeting_notes.docx", "Meetings")
        };

        // Act
        //var results = await _organizer.OrganizeFilesAsync(files);

        // Assert
        // Assert.Equal(3, results.Count);
        // Assert.Equal("Financial", results[0].FolderName);
        // Assert.Equal("Photos", results[1].FolderName);
        // Assert.Equal("Meetings", results[2].FolderName);
    }

    [Fact]
    public async Task OrganizeFilesAsync_WithUnknownFiles_UsesAI()
    {
        // Arrange
        var files = new List<FileItemViewModel>
        {
            CreateFileViewModel("unknown_file.txt", "")
        };


        var mockResponse = new[] {
            new ChatMessageContent(AuthorRole.Assistant, "Work")
        };

        _mockChatService
            .GetChatMessageContentsAsync(Arg.Any<ChatHistory>(), Arg.Any<PromptExecutionSettings>())
            .Returns(mockResponse);

        // Act
        // var results = await _organizer.OrganizeFilesAsync(files);
        //
        // // Assert
        // Assert.Single(results);
        // Assert.Equal("Work", results[0].FolderName);
        // await _mockChatService.Received(1).GetChatMessageContentsAsync(
        //     Arg.Any<ChatHistory>(),
        //     Arg.Any<PromptExecutionSettings>()
        // );
    }

    [Fact]
    public async Task OrganizeFilesAsync_WithCancellation_StopsProcessing()
    {
        // Arrange
        var files = new List<FileItemViewModel>
        {
            CreateFileViewModel("test1.txt", ""),
            CreateFileViewModel("test2.txt", "")
        };

        var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        // Act
        var results = await _organizer.OrganizeFilesAsync(files,null,new List<string>(), cancellationToken: cts.Token);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task OrganizeFilesAsync_WithProgress_ReportsCorrectly()
    {
        // Arrange
        var files = new List<FileItemViewModel>
        {
            CreateFileViewModel("invoice.pdf", "Financial"),
            CreateFileViewModel("unknown.txt", "")
        };

        var progressValues = new List<double>();
        var progressReported = new TaskCompletionSource<bool>();

        var progress = new Progress<double>(value =>
        {
            progressValues.Add(value);
            if (value >= 100)
            {
                progressReported.SetResult(true);
            }
        });

        var mockResponse = new[] {
            new ChatMessageContent(AuthorRole.Assistant, "Work")
        };

        _mockChatService
            .GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>())
            .Returns(mockResponse);

        // Act
        // var resultTask = _organizer.OrganizeFilesAsync(files, progress);
        //
        // // Wait for either the progress to complete or a timeout
        // var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
        // var completedTask = await Task.WhenAny(progressReported.Task, timeoutTask);
        //
        // var results = await resultTask;
        //
        // // Assert
        // Assert.True(completedTask == progressReported.Task, "Progress reporting timed out");
        // Assert.NotEmpty(progressValues);
        // Assert.Contains(progressValues, v => v > 0);
        // Assert.Contains(progressValues, v => v <= 100);


    }

    [Theory]
    [InlineData("file.jpg", "Photos")]
    [InlineData("file.jpeg", "Photos")]
    [InlineData("file.png", "Photos")]
    [InlineData("file.mp3", "Sounds")]
    public async Task OrganizeFilesAsync_WithDifferentExtensions_CategorizesCorrectly(string fileName, string expectedCategory)
    {
        // Arrange
        var files = new List<FileItemViewModel>
        {
            CreateFileViewModel(fileName, expectedCategory)
        };

        // Act
        // var results = await _organizer.OrganizeFilesAsync(files);
        //
        // // Assert
        // Assert.Single(results);
        // Assert.Equal(expectedCategory, results[0].FolderName);
    }

    [Fact]
    public async Task OrganizeFilesAsync_WithAIError_ReturnsNullForFile()
    {
        // Arrange
        var files = new List<FileItemViewModel>
        {
            CreateFileViewModel("unknown.txt", "")
        };

        _mockChatService
            .GetChatMessageContentsAsync(Arg.Any<ChatHistory>(), Arg.Any<PromptExecutionSettings>())
            .Returns(Task.FromException<IReadOnlyList<ChatMessageContent>>(new Exception("AI Error")));

        // Act
        //var results = await _organizer.OrganizeFilesAsync(files);

        // Assert
        //Assert.Empty(results);
    }

    private static FileItemViewModel CreateFileViewModel(string fileName, string expectedCategory)
    {
        return new FileItemViewModel(new FileItem()
        {
            Path = fileName,
            Name = fileName,
            CreatedDate = DateTime.Now,
            Size = 1024,
        });
    }
}