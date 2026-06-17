namespace FileForge.Domain.Models;

public sealed class ConsolidationDecision
{
    public string RelativePath { get; set; } = string.Empty;

    public string SelectedSourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastModifiedTime { get; set; }

    public string Sha256Hash { get; set; } = string.Empty;
}