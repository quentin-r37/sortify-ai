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

namespace FileOrganizer.ViewModels;

public partial class Step4ViewModel : StepViewModel
{
    private readonly AnalysisViewModel _parent;

    [ObservableProperty]
    private ISeries[] _dataSeries;

    [ObservableProperty]
    private Axis[] _xAxes;

    [ObservableProperty]
    private Axis[] _yAxes;

    [ObservableProperty]
    private SolidColorPaint _legendTextPaint;

    public Step4ViewModel(AnalysisViewModel parent, MainWindow window, IServiceProvider serviceProvider)
    {
        StepNumber = 4;
        Icon = "fa-brain";
        Description = "Step 4";
        _parent = parent;
    }

    public async Task PreparePreviewAsync(OrganizationType organizationType)
    {
        try
        {
            var vectorAsyncEnumerable = _parent.VectorCollection.GetBatchAsync(_parent.Keys);
            var data = new List<DocumentEmbedding>();
            await foreach (var vectorFile in vectorAsyncEnumerable)
            {
                data.Add(new DocumentEmbedding()
                {
                    Embedding = vectorFile.Embedding.ToArray(),
                    Id = vectorFile.Id,
                    Paragraph = vectorFile.Text
                });
            }
            var mlContext = new MLContext();
            var dataView = mlContext.Data.LoadFromEnumerable(data);

            var pipeline = mlContext.Transforms
                .NormalizeMeanVariance(
                    inputColumnName: nameof(DocumentEmbedding.Embedding),
                    outputColumnName: "NormalizedFeatures")
                .Append(mlContext.Transforms.ProjectToPrincipalComponents(
                    outputColumnName: "PCAFeatures",
                    inputColumnName: "NormalizedFeatures",
                    rank: 2));

            var pcaTransformer = pipeline.Fit(dataView);
            var pcaDataView = pcaTransformer.Transform(dataView);

            var clusteringPipeline = mlContext.Clustering.Trainers.KMeans(
                featureColumnName: "PCAFeatures",
                numberOfClusters: 6
            );

            var model = clusteringPipeline.Fit(pcaDataView);
            var predictions = model.Transform(pcaDataView);

            var embeddingWithPredictions = mlContext.Data.CreateEnumerable<DocumentEmbeddingWithPredictionPca>(
                predictions,
                reuseRowObject: false
            ).ToList();

            var clusterCategories = await AssignCategoriesToClustersAsync(embeddingWithPredictions, data);
            OrganizeFilesByCategories(embeddingWithPredictions, clusterCategories);

            await CreateClusterVisualizationAsync(embeddingWithPredictions);
            InitializeChartConfiguration();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in PreparePreview: {ex}");
        }
    }
    private void InitializeChartConfiguration()
    {
        XAxes =
        [
            new Axis
            {
                Name = "Principal Component 1",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 0.5f }
            }
        ];

        YAxes =
        [
            new Axis()
            {
                Name = "Principal Component 2",
                NamePaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 0.5f }
            }
        ];

        LegendTextPaint = new SolidColorPaint
        {
            Color = SKColors.Gray,
            SKTypeface = SKTypeface.FromFamilyName("Arial")
        };
    }

    private async Task CreateClusterVisualizationAsync(List<DocumentEmbeddingWithPredictionPca> predictions)
    {
        await Task.Run(() =>
        {
            // Group data by cluster and create a dictionary with both points and document IDs
            var clusterData = predictions
                .GroupBy(p => p.PredictedLabel)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(item => (
                        Point: new ObservablePoint(item.PcaFeatures[0], item.PcaFeatures[1]),
                        Id: item.Id))
                        .ToList()
                );

            var colors = new[]
            {
            SKColors.DodgerBlue,
            SKColors.Crimson,
            SKColors.ForestGreen,
            SKColors.DarkOrange,
            SKColors.MediumPurple,
            SKColors.Chocolate
        };

            var series = clusterData
                .Select((cluster, index) =>
                {
                    var points = cluster.Value.Select(x => x.Point).ToList();
                    var ids = cluster.Value.Select(x => x.Id).ToList();

                    return new ScatterSeries<ObservablePoint>
                    {
                        Name = $"Cluster {cluster.Key + 1}",
                        Values = points,
                        Fill = new SolidColorPaint(colors[index % colors.Length]),
                        GeometrySize = 10,
                        XToolTipLabelFormatter = (chartPoint) =>
                        {
                            var pointIndex = points.IndexOf((ObservablePoint)chartPoint.Model);
                            var documentId = ids[pointIndex];

                            var sb = new StringBuilder();
                            sb.AppendLine($"Cluster {cluster.Key + 1}");
                            sb.AppendLine($"Document ID: {documentId}");
                            sb.AppendLine($"PC1: {chartPoint.Coordinate.PrimaryValue:F2}");
                            sb.AppendLine($"PC2: {chartPoint.Coordinate.SecondaryValue:F2}");
                            return sb.ToString();
                        }
                    };
                })
                .ToArray<ISeries>();

            DataSeries = series;
        });
    }

    private async Task<Dictionary<uint, string>> AssignCategoriesToClustersAsync(
        List<DocumentEmbeddingWithPredictionPca> predictions,
        List<DocumentEmbedding> data)
    {
        var clusterParagraphs = new Dictionary<uint, List<string>>();

        foreach (var item in predictions)
        {
            if (!clusterParagraphs.ContainsKey(item.PredictedLabel))
                clusterParagraphs[item.PredictedLabel] = [];

            var paragraph = data.FirstOrDefault(d => d.Id == item.Id)?.Paragraph;

            if (!string.IsNullOrEmpty(paragraph))
            {
                clusterParagraphs[item.PredictedLabel].Add(paragraph);
            }
        }

        var clusterCategories = new Dictionary<uint, string>();

        foreach (var cluster in clusterParagraphs)
        {
            var sampleParagraphs = cluster.Value.Take(10).ToList();
            var category = await GetClusterCategoryAsync(sampleParagraphs);
            clusterCategories[cluster.Key] = category;
        }

        return clusterCategories;
    }

    private async Task<string> GetClusterCategoryAsync(List<string> paragraphs)
    {
        var prompt = $"""
                          À partir des exemples suivants, pouvez-vous identifier la catégorie ou le thème principal de ce groupe (deux ou trois mots maximum)?
                          Pensez à des catégories de fichiers, comme : factures, courrier, scripts, pièce d'identité, etc.
                          Exemples:
                          {paragraphs.Aggregate((current, paragraph) => current + $"- {paragraph}\n")}
                      """;

        var test = await _parent.ChatCompletionService.GetChatMessageContentsAsync([
            new ChatMessageContent(AuthorRole.System,
                "Tu es un assistant qui aide à identifier la catégorie la plus pertinente pour classer des fichiers en fonction de leur contenu. Les catégories doivent être concises et représenter le type de documents, comme 'factures', 'courrier', 'pièce d'identité', etc."),
            new ChatMessageContent(AuthorRole.User, prompt)
        ]);

        var sb = new StringBuilder();
        foreach (var chatMessageContent in test)
        {
            sb.Append(chatMessageContent.Content);
        }
        var response = sb.ToString();
        return response;
    }

    private void OrganizeFilesByCategories(
        List<DocumentEmbeddingWithPredictionPca> predictions,
        Dictionary<uint, string> clusterCategories)
    {
        var fileCategories = new Dictionary<string, List<string>>();
        foreach (var item in predictions)
        {
            var category = clusterCategories[item.PredictedLabel];

            if (!fileCategories.ContainsKey(item.Id))
                fileCategories[item.Id] = [];

            if (!fileCategories[item.Id].Contains(category))
                fileCategories[item.Id].Add(category);
        }

        //foreach (var fileEntry in fileCategories)
        //{
        //    var fileId = fileEntry.Key;
        //    var categories = fileEntry.Value;
        //    var filePath = GetFilePathById(fileId);
        //    foreach (var category in categories)
        //    {
        //        var destinationFolder = Path.Combine("VotreCheminDeBase", category);
        //        if (!Directory.Exists(destinationFolder))
        //            Directory.CreateDirectory(destinationFolder);

        //        var destinationPath = Path.Combine(destinationFolder, Path.GetFileName(filePath));
        //        if (!File.Exists(destinationPath))
        //        {
        //            File.Copy(filePath, destinationPath);
        //        }
        //    }
        //}
    }

    public class DocumentEmbedding
    {
        [VectorType(384)]
        public float[] Embedding { get; set; }

        public string Id { get; set; }
        public string Paragraph { get; set; }
    }

    public class DocumentEmbeddingWithPredictionPca
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }

        public string Id { get; set; }

        [VectorType(2)]
        [ColumnName("PCAFeatures")]
        public float[] PcaFeatures { get; set; }
    }

    private void ExecuteOrganization()
    {
        // Perform the file organization
        // Save the analysis to the database
    }
}