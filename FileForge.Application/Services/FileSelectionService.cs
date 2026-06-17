using FileForge.Domain.Models;

namespace FileForge.Application.Services;

public sealed class FileSelectionService
{
    public List<ConsolidationGroup> BuildGroups(List<SourceFileRecord> files)
    {
        return BuildGroups(files, Array.Empty<string>());
    }

    public List<ConsolidationGroup> BuildGroups(List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
    {
        return files
            .GroupBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(g => BuildGroup(g.Key, g.ToList(), sourceRootOrder))
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ConsolidationGroup BuildGroup(string relativePath, List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
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
            group.DecisionReason = "Only one file found at this relative path. Hash not required for uniqueness.";
            return group;
        }

        if (files.Any(f => !f.HashCalculated))
        {
            group.SelectedFile = SelectBestCandidate(files, sourceRootOrder);
            group.Status = ConsolidationStatus.Error;
            group.DecisionReason = "One or more files with the same relative path could not be hashed. Review before copying.";
            return group;
        }

        int distinctHashCount = files
            .Select(f => f.Sha256Hash)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (distinctHashCount == 1)
        {
            group.SelectedFile = SelectFirstSourceCandidate(files, sourceRootOrder);
            group.Status = ConsolidationStatus.DuplicateSameContent;
            group.DecisionReason = "Same relative path and same SHA256 content hash. First selected source root wins; other copies are skipped.";
            return group;
        }

        group.SelectedFile = SelectBestCandidate(files, sourceRootOrder);
        group.Status = ConsolidationStatus.ConflictDifferentContent;
        group.DecisionReason = "Same relative path but different SHA256 content hashes. Latest modified/larger file shown as provisional winner; manual review recommended.";
        return group;
    }

    private static SourceFileRecord SelectFirstSourceCandidate(List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
    {
        return files
            .OrderBy(f => SourceOrderIndex(f.SourceRoot, sourceRootOrder))
            .ThenByDescending(f => f.LastModifiedTime)
            .ThenByDescending(f => f.SizeBytes)
            .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static SourceFileRecord SelectBestCandidate(List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
    {
        return files
            .OrderByDescending(f => f.LastModifiedTime)
            .ThenByDescending(f => f.SizeBytes)
            .ThenBy(f => SourceOrderIndex(f.SourceRoot, sourceRootOrder))
            .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static int SourceOrderIndex(string sourceRoot, IReadOnlyList<string> sourceRootOrder)
    {
        for (int i = 0; i < sourceRootOrder.Count; i++)
        {
            if (string.Equals(sourceRootOrder[i], sourceRoot, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return int.MaxValue;
    }
}
