namespace FileForge.Domain.Models;

public sealed class SourceFileRecord
{
    public string SourceRoot { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string DirectoryPath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime LastModifiedTime { get; set; }

    public string Sha256Hash { get; set; } = string.Empty;

    public bool HashCalculated { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}