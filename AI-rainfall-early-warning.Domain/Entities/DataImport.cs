namespace AI_rainfall_early_warning.Domain.Entities;

public class DataImport
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public string? ImportedBy { get; set; }
    public int RowsImported { get; set; }
    public int RowsRejected { get; set; }
    public string? ErrorsSummary { get; set; }
}