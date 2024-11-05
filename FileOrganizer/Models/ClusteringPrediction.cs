using Microsoft.ML.Data;

namespace FileOrganizer.Models;

public class ClusteringPrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedClusterId { get; set; }

    public float[] Score { get; set; }
}