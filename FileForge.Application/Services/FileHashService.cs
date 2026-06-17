using FileForge.Domain.Models;
using FileForge.Infrastructure.Hashing;

namespace FileForge.Application.Services;

public sealed class FileHashService
{
    private readonly Sha256FileHasher _hasher = new();

    public void CalculateHashes(IEnumerable<SourceFileRecord> files)
    {
        foreach (SourceFileRecord file in files)
        {
            CalculateHash(file);
        }
    }

    public int CalculateRequiredHashes(IEnumerable<SourceFileRecord> files)
    {
        List<SourceFileRecord> filesToHash = files
            .GroupBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Where(f => !f.HashCalculated)
            .ToList();

        foreach (SourceFileRecord file in filesToHash)
        {
            CalculateHash(file);
        }

        return filesToHash.Count;
    }

    private void CalculateHash(SourceFileRecord file)
    {
        try
        {
            file.Sha256Hash = _hasher.ComputeHash(file.FullPath);
            file.HashCalculated = true;
            file.ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            file.Sha256Hash = string.Empty;
            file.HashCalculated = false;
            file.ErrorMessage = ex.Message;
        }
    }
}
