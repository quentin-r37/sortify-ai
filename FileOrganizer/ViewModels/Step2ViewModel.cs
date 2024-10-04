using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Models;
using FileOrganizer.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace FileOrganizer.ViewModels;

public partial class Step2ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;
    public ObservableCollection<FileItemViewModel> Files { get; set; }

    [ObservableProperty]
    private int _fileTypesCount = 10;

    public int MaxFileTypes => Files.Select(f => f.FileType).Distinct().Count();

    partial void OnFileTypesCountChanged(int value)
    {
        CalculateFilesByType();
    }

    // Properties for charts (e.g., SizeByTypeChartData)

    public Step2ViewModel(AnalysisViewModel parent, MainWindow? window)
    {
        _parent = parent;
        Files = new ObservableCollection<FileItemViewModel>();
    }

    public void LoadFiles(string? path)
    {
        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Select(filePath => new FileItemViewModel(new FileItem
            {
                Path = filePath,
                Name = System.IO.Path.GetFileName((string?)filePath),
                Size = new FileInfo(filePath).Length,
                CreatedDate = File.GetCreationTime((string)filePath),
                ModifiedDate = File.GetLastWriteTime((string)filePath),
                FileType = System.IO.Path.GetExtension((string?)filePath)
            }));
        Files.Clear();
        foreach (var file in files)
        {
            Files.Add(file);
        }
        CalculateChartsData();
    }

    public ISeries[] FilesByMonthSeries { get; set; }
    public Axis[] FilesByMonthXAxis { get; set; }
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

        FilesByMonthXAxis = new Axis[]
        {
            new Axis
            {
                Labels = groupedByMonth.Select(g => g.Key).ToArray(),
                LabelsRotation = 45,
                SeparatorsPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                SeparatorsAtCenter = false,
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                TicksAtCenter = true
            }
        };

        FilesByMonthYAxes =
        [
            new Axis // First Y-axis for count
            {
                Name = "File Count",
                Position = LiveChartsCore.Measure.AxisPosition.Start,
                SeparatorsPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
            },

            new Axis // Second Y-axis for size
            {
                Name = "Size (MB)",
                Position = LiveChartsCore.Measure.AxisPosition.End,
                SeparatorsPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
            }
        ];
    }

    private static readonly SKColor CountColor = new SKColor(25, 118, 210); // Blue
    private static readonly SKColor SizeColor = new SKColor(198, 40, 40);   // Red
    private static readonly SKColor SizeColorFill = new SKColor(198, 40, 40, 0);   // Red

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
                Stroke = new SolidColorPaint(CountColor)
            },
            new LineSeries<double>
            {
                Name = "Total Size (MB)",
                Values = sizeValues,
                ScalesYAt = 1, // Use the second Y-axis (right, for size)
                Stroke = new SolidColorPaint(SizeColor){StrokeThickness = 4},
                Fill = new SolidColorPaint(SizeColorFill),
                GeometryFill = new SolidColorPaint(SizeColor),
                GeometryStroke =  new SolidColorPaint(SizeColor),
                GeometrySize = 8
            }
        };

        FilesByTypeXAxis =
        [
            new Axis
            {
                Labels = groupedByType.Select(g => g.Key).ToArray(),
                LabelsRotation = 45,
                SeparatorsPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                SeparatorsAtCenter = false,
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                TicksAtCenter = true
            }
        ];

        FilesByTypeYAxes =
        [
            new Axis // First Y-axis for count
            {
                Name = "File Count",
                Position = LiveChartsCore.Measure.AxisPosition.Start,
                SeparatorsPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
            },

            new Axis // Second Y-axis for size
            {
                Name = "Size (MB)",
                Position = LiveChartsCore.Measure.AxisPosition.End,
                SeparatorsPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
                TicksPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
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