using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Database;
using FileOrganizer.Shared;
using FileOrganizer.Views;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Ollama;
using SkiaSharp;

namespace FileOrganizer.ViewModels;

public partial class Step2ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    public List<FileItemViewModel> Files { get; set; }
    private List<FileItemViewModel> _allFiles { get; set; }

    public DataGridCollectionView GroupedGridData { get; set; }

    [ObservableProperty]
    private int _fileTypesCount = 10;

    [ObservableProperty]
    private bool _filterAllFiles = true;

    partial void OnFilterAllFilesChanged(bool value)
    {
        RefreshFilesFilter();
    }

    public SolidColorPaint LegendTextPaint { get; set; } = new SolidColorPaint(new SKColor(255, 255, 255));

    public int MaxFileTypes => Files.Select(f => f.FileType).Distinct().Count();

    partial void OnFileTypesCountChanged(int value)
    {
        CalculateFilesByType();
    }

    public Step2ViewModel(AnalysisViewModel parent, MainWindow? window)
    {
        _parent = parent;
        Files = [];
    }

    public async Task ExtractDocFiles()
    {
        using var ollama = new OllamaApiClient();
        var embedding = await ollama.Embeddings.GenerateEmbeddingAsync(
            model: "all-minilm",
            prompt: "hello");

    }

    public void LoadFiles(string path)
    {
        _allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Select(filePath =>
            {
                var file = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(path, file.DirectoryName);
                return new FileItemViewModel(new FileItem
                {
                    Path = filePath,
                    Name = Path.GetFileName(filePath),
                    Size = file.Length,
                    CreatedDate = file.CreationTime,
                    ModifiedDate = file.LastWriteTime,
                    FileType = Path.GetExtension(filePath),
                    DirectoryName = relativePath == "." ? "" : relativePath
                });
            }).ToList();

        RefreshFilesFilter();
    }

    private void RefreshFilesFilter()
    {
        var filtered = _allFiles;
        if (!FilterAllFiles)
        {
            filtered = _allFiles.Where(model => model.FileType == ".docx").ToList();
        }
        Files.Clear();
        Files.AddRange(filtered);
        GroupedGridData = new DataGridCollectionView(Files);
        GroupedGridData.GroupDescriptions.Add(new DataGridPathGroupDescription("DirectoryName"));
        CalculateChartsData();
    }

    public ISeries[] FilesByMonthSeries { get; set; }
    public ObservableCollection<Axis> FilesByMonthXAxis { get; set; }
    public ObservableCollection<Axis> FilesByMonthYAxes { get; set; }

    public ISeries[] FilesByTypeSeries { get; set; }
    public ObservableCollection<Axis> FilesByTypeXAxis { get; set; }
    public ObservableCollection<Axis> FilesByTypeYAxes { get; set; }

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
                ScalesYAt = 0 // Use the first Y-axis (left, for count)
            },
            new LineSeries<double>
            {
                Name = "Total Size (MB)",
                Values = sizeValues,
                ScalesYAt = 1 // Use the second Y-axis (right, for size)
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
            new Axis // First Y-axis for count
            {
                Name = "File Count",
                LabelsPaint =  new SolidColorPaint(new SKColor(255, 255, 255)),
                Position = LiveChartsCore.Measure.AxisPosition.Start,
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint =    new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0
            },

            new Axis // Second Y-axis for size
            {
                Name = "Size (MB)",
                LabelsPaint =  new SolidColorPaint(new SKColor(255, 255, 255)),
                Position = LiveChartsCore.Measure.AxisPosition.End,
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint =    new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0
            }
        ];

        OnPropertyChanged(nameof(FilesByMonthSeries));
        OnPropertyChanged(nameof(FilesByMonthXAxis));
        OnPropertyChanged(nameof(FilesByMonthYAxes));
    }

    private static readonly SKColor CountColor = new SKColor(25, 118, 210);
    private static readonly SKColor SizeColor = new SKColor(198, 40, 40);
    private static readonly SKColor SizeColorFill = new SKColor(198, 40, 40, 20);

    private void CalculateFilesByType()
    {
        var groupedByType = Files
            .GroupBy(f => string.IsNullOrEmpty(f.FileType) ? "No extension" : f.FileType.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(FileTypesCount) //
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
                Stroke = new SolidColorPaint(CountColor),
            },
            new LineSeries<double>
            {
                Name = "Total Size (MB)",
                Values = sizeValues,
                ScalesYAt = 1, // Use the second Y-axis (right, for size)
                Stroke = new SolidColorPaint(SizeColor){StrokeThickness = 4},
                Fill = new SolidColorPaint(SizeColorFill),
                GeometryFill = new SolidColorPaint(new SKColor(255,255,255)),
                GeometryStroke =  new SolidColorPaint(SizeColor){StrokeThickness = 4},
                GeometrySize = 10,
            }
        };

        FilesByTypeXAxis =
        [
            new Axis
            {
                Labels = groupedByType.Select(g => g.Key).ToArray(),
                LabelsRotation = 45,
                LabelsPaint =  new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                SeparatorsAtCenter = false,
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                TicksAtCenter = true,
            }
        ];

        FilesByTypeYAxes =
        [
            new Axis // First Y-axis for count
            {
                Name = "File Count",
                Position = LiveChartsCore.Measure.AxisPosition.Start,
                LabelsPaint =  new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint =    new SolidColorPaint(new SKColor(255, 255, 255)),
                MinLimit = 0

            },

            new Axis // Second Y-axis for size
            {
                Name = "Size (MB)",
                Position = LiveChartsCore.Measure.AxisPosition.End,
                LabelsPaint =  new SolidColorPaint(new SKColor(255, 255, 255)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                NamePaint =    new SolidColorPaint(new SKColor(255, 255, 255)),
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
        _parent.NextStep();
    }

    [RelayCommand]
    private void Previous()
    {
        _parent.PreviousStep();
    }



}