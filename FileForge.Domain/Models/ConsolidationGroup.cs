namespace FileForge.Domain.Models;

public sealed class ConsolidationGroup
{
    public string RelativePath { get; set; } = string.Empty;

    public List<SourceFileRecord> Files { get; set; } = new();

    public SourceFileRecord? SelectedFile { get; set; }

    public ConsolidationStatus Status { get; set; }

    public string DecisionReason { get; set; } = string.Empty;
    public int FileCount => Files.Count;
}

public enum ConsolidationStatus
{
    Unknown = 0,
    Unique = 1,
    DuplicateSameContent = 2,
    ConflictDifferentContent = 3,
    Error = 4
}