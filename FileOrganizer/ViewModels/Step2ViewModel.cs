using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Services;
using FileOrganizer.Shared;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace FileOrganizer.ViewModels;

public partial class Step2ViewModel : StepViewModel
{
    private static readonly SKColor CountColor = new(25, 118, 210);
    private static readonly SKColor SizeColor = new(198, 40, 40);
    private static readonly SKColor SizeColorFill = new(198, 40, 40, 20);
    private readonly AnalysisViewModel _parent;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private int _fileTypesCount = 10;
    [ObservableProperty] private bool _filterAllFiles = true;
    [ObservableProperty] private bool _showSubDirectories = true;
    [ObservableProperty] private ZoomAndPanMode _zoomModeByDate = ZoomAndPanMode.None;
    [ObservableProperty] private ZoomAndPanMode _zoomModeByType = ZoomAndPanMode.None;

    public List<string> SortTypes { get; set; } = ["By count", "By size"];
    [ObservableProperty] private string _sortTypeSelected = "By count";

    public List<ZoomAndPanMode> ZoomAndPanModes { get; set; } = [ZoomAndPanMode.None, ZoomAndPanMode.X];

    public Step2ViewModel(AnalysisViewModel parent, IServiceProvider serviceProvider)
    {
        StepNumber = 2;
        Icon = "fa-chart-simple";
        Description = "Step 2";
        _parent = parent;
        _serviceProvider = serviceProvider;
        Files = [];
    }

    public ObservableCollection<FileItemViewModel> Files { get; set; }
    private List<FileItemViewModel> AllFiles { get; set; }

    [ObservableProperty]
    private DataGridCollectionView _groupedGridData;

    public SolidColorPaint LegendTextPaint { get; set; } = new(new SKColor(255, 255, 255));
    public int MaxFileTypes => Files.Select(f => f.FileType).Distinct().Count();

    public ISeries[] FilesByMonthSeries { get; set; }
    public ObservableCollection<Axis> FilesByMonthXAxis { get; set; }
    public ObservableCollection<Axis> FilesByMonthYAxes { get; set; }

    public ISeries[] FilesByTypeSeries { get; set; }
    public ObservableCollection<Axis> FilesByTypeXAxis { get; set; }
    public ObservableCollection<Axis> FilesByTypeYAxes { get; set; }


    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public Step2ViewModel()
    {
    }

    partial void OnFilterAllFilesChanged(bool value)
    {
        RefreshFilesFilter();
    }

    partial void OnShowSubDirectoriesChanged(bool value)
    {
        RefreshFilesFilter();
    }

    partial void OnFileTypesCountChanged(int value)
    {
        CalculateFilesByType();
    }

    partial void OnSortTypeSelectedChanged(string value)
    {
        CalculateFilesByType();
    }

    public async Task ExtractTextAndGenerateEmbeddingsAsync()
    {
        var textExtractionService = _serviceProvider.GetRequiredService<ITextExtractionService>();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsProcessing = true;
            Progress = 0;
            StatusMessage = "Starting text extraction...";
        });
        try
        {
            var progress = new Progress<(int Processed, int Total, string Message)>(report =>
            {
                Progress = (double)report.Processed / report.Total * 100;
                StatusMessage = report.Message;
            });

            await textExtractionService.ExtractAndEmbedAsync(
                _parent.Model.Id,
                _parent.VectorCollection,
                _parent.TextEmbeddingGenerationService,
                _parent.Keys,
                progress,
                AllFiles);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"An error occurred: {ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsProcessing = false;
            });
        }
    }
    public void LoadFiles(string path)
    {
        var fileList = new List<FileItemViewModel>();

        try
        {
            var directories = new List<string> { path };
            try
            {
                directories.AddRange(Directory.GetDirectories(path, "*", SearchOption.AllDirectories));
            }
            catch (UnauthorizedAccessException)
            {
            }

            foreach (var directory in directories)
            {
                try
                {
                    var files = Directory.GetFiles(directory);
                    foreach (var filePath in files)
                    {
                        try
                        {
                            var file = new FileInfo(filePath);
                            var relativePath = Path.GetRelativePath(path, file.DirectoryName);
                            var dir = relativePath.Split(Path.DirectorySeparatorChar).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();

                            fileList.Add(new FileItemViewModel(new FileItem
                            {
                                Path = filePath,
                                Name = Path.GetFileName(filePath),
                                Size = file.Length,
                                CreatedDate = file.CreationTime,
                                ModifiedDate = file.LastWriteTime,
                                FileType = Path.GetExtension(filePath),
                                DirectoryName = relativePath == "." ? "" : relativePath,
                                DirectoryLevels = dir
                            }));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing file {filePath}: {ex.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error accessing directory {directory}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during file loading: {ex.Message}");
        }

        AllFiles = fileList;
        RefreshFilesFilter();
    }

    private void RefreshFilesFilter()
    {
        var filtered = AllFiles;
        if (!FilterAllFiles) filtered = AllFiles.WhereFileTypeIsDocument();
        Files = new ObservableCollection<FileItemViewModel>(filtered);
        GroupedGridData = new DataGridCollectionView(Files);
        if (ShowSubDirectories)
        {
            GroupedGridData.GroupDescriptions.Add(new DataGridPathGroupDescription("DirectoryName"));
        }
        GroupedGridData.Refresh();
        CalculateChartsData();
    }

    private void CalculateChartsData()
    {
        CalculateFilesByMonth();
        CalculateFilesByType();
    }

    private void CalculateFilesByMonth()
    {
        var groupedByMonth = Files
            .GroupBy(f => f.CreatedDate.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToList();

        var countValues = groupedByMonth.Select(g => (double)g.Count()).ToArray();
        var sizeValues = groupedByMonth.Select(g => (double)g.Sum(f => f.Size) / 1024 / 1024).ToArray(); // Size in MB

        FilesByMonthSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Number of Files",
                Values = countValues,
                ScalesYAt = 0
            },
            new LineSeries<double>
            {
                Name = "Total Size (MB)",
                Values = sizeValues,
                ScalesYAt = 1
            }
        };

        FilesByMonthXAxis =
        [
            new Axis
            {
                Labels = groupedByMonth.Select(g => g.Key).ToArray(),
                LabelsRotation = 45,
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                SeparatorsAtCenter = false,
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                TicksAtCenter = true
            }
        ];

        FilesByMonthYAxes =
        [
            new Axis
            {
                Name = "File Count",
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                Position = AxisPosition.Start,
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0
            },

            new Axis
            {
                Name = "Size (MB)",
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                Position = AxisPosition.End,
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0
            }
        ];

        OnPropertyChanged(nameof(FilesByMonthSeries));
        OnPropertyChanged(nameof(FilesByMonthXAxis));
        OnPropertyChanged(nameof(FilesByMonthYAxes));
    }

    private void CalculateFilesByType()
    {
        var query = Files
            .GroupBy(f => string.IsNullOrEmpty(f.FileType) ? "No extension" : f.FileType.ToLower());

        query = SortTypeSelected == "By count" ? query.OrderByDescending(g => g.Count()) : query.OrderByDescending(g => g.Sum(model => model.Size));

        var groupedByType = query.Take(FileTypesCount) //
            .ToList();

        var countValues = groupedByType.Select(g => (double)g.Count()).ToArray();
        var sizeValues = groupedByType.Select(g => (double)g.Sum(f => f.Size) / 1024 / 1024).ToArray(); // Size in MB

        FilesByTypeSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Number of Files",
                Values = countValues,
                ScalesYAt = 0, // Use the first Y-axis (left, for count
                Fill = new SolidColorPaint(CountColor),
                Stroke = new SolidColorPaint(CountColor)
            },
            new LineSeries<double>
            {
                Name = "Total Size (MB)",
                Values = sizeValues,
                ScalesYAt = 1, // Use the second Y-axis (right, for size)
                Stroke = new SolidColorPaint(SizeColor) { StrokeThickness = 4 },
                Fill = new SolidColorPaint(SizeColorFill),
                GeometryFill = new SolidColorPaint(new SKColor(255, 255, 255)),
                GeometryStroke = new SolidColorPaint(SizeColor) { StrokeThickness = 4 },
                GeometrySize = 10
            }
        };

        FilesByTypeXAxis =
        [
            new Axis
            {
                Labels = groupedByType.Select(g => g.Key).ToArray(),
                LabelsRotation = 45,
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                SeparatorsAtCenter = false,
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                TicksAtCenter = true
            }
        ];

        FilesByTypeYAxes =
        [
            new Axis
            {
                Name = "File Count",
                Position = AxisPosition.Start,
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0
            },

            new Axis
            {
                Name = "Size (MB)",
                Position = AxisPosition.End,
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0
            }
        ];

        OnPropertyChanged(nameof(FilesByTypeSeries));
        OnPropertyChanged(nameof(FilesByTypeXAxis));
        OnPropertyChanged(nameof(FilesByTypeYAxes));
    }


    [RelayCommand]
    private void Next()
    {
        _parent.Step3.LoadFileList(AllFiles.WhereFileTypeIsDocument());
        _parent.NextStep();
    }

    [RelayCommand]
    private void Previous()
    {
        _parent.PreviousStep();
    }
}