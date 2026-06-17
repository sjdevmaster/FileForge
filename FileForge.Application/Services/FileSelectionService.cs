using FileForge.Domain.Models;

namespace FileForge.Application.Services;

public sealed class FileSelectionService
{
    public List<ConsolidationGroup> BuildGroups(List<SourceFileRecord> files)
    {
        return files
            .GroupBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(g => BuildGroup(g.Key, g.ToList()))
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ConsolidationGroup BuildGroup(string relativePath, List<SourceFileRecord> files)
    {
        ConsolidationGroup group = new()
        {
            RelativePath = relativePath,
            Files = files
        };

        if (files.Count == 1)
        {
            group.SelectedFile = files[0];
            group.Status = ConsolidationStatus.Unique;
            group.DecisionReason = "Only one file found across selected source roots.";
            return group;
        }

        if (files.Any(f => !f.HashCalculated))
        {
            group.Status = ConsolidationStatus.Error;
            group.DecisionReason = "One or more files could not be hashed. This group cannot be safely consolidated.";
            return group;
        }

        int distinctHashCount = files
            .Select(f => f.Sha256Hash)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (distinctHashCount == 1)
        {
            group.SelectedFile = files
                .OrderBy(f => f.SourceRoot, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
                .First();

            group.Status = ConsolidationStatus.DuplicateSameContent;
            group.DecisionReason = "Same relative path and same content hash. First selected source root is used as the predictable winner; other copies are skipped.";
            return group;
        }

        group.SelectedFile = files
            .OrderByDescending(f => f.LastModifiedTime)
            .ThenByDescending(f => f.SizeBytes)
            .ThenBy(f => f.SourceRoot, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
            .First();

        group.Status = ConsolidationStatus.ConflictDifferentContent;
        group.DecisionReason = "Same relative path but different content hash. Latest modified file is selected temporarily. Manual review is recommended before final archival.";
        return group;
    }
}
