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

    private static ConsolidationGroup BuildGroup(
        string relativePath,
        List<SourceFileRecord> files,
        IReadOnlyList<string> sourceRootOrder)
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

        int distinctSizeCount = files
            .Select(f => f.SizeBytes)
            .Distinct()
            .Count();

        if (distinctSizeCount > 1)
        {
            group.SelectedFile = SelectLatestModifiedCandidate(files, sourceRootOrder);
            group.Status = ConsolidationStatus.ConflictDifferentContent;
            group.DecisionReason =
                "Conflict auto-resolved. Main archive version selected by latest modified date. " +
                "Older-dated conflicting versions preserved under _FileForge_Conflicts during Copy. " +
                "File sizes differ, so content differs; hash not required.";
            return group;
        }

        if (files.Any(f => !f.HashCalculated))
        {
            group.SelectedFile = SelectLatestModifiedCandidate(files, sourceRootOrder);
            group.Status = ConsolidationStatus.Error;
            group.DecisionReason =
                "One or more same-size files with the same relative path could not be hashed. " +
                "Copy is blocked for this group until the file read/hash issue is resolved.";
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
            group.DecisionReason =
                "Same relative path, same file size, and same SHA256 content hash. " +
                "First selected source root wins; other copies are skipped.";
            return group;
        }

        group.SelectedFile = SelectLatestModifiedCandidate(files, sourceRootOrder);
        group.Status = ConsolidationStatus.ConflictDifferentContent;
        group.DecisionReason =
            "Conflict auto-resolved. Main archive version selected by latest modified date. " +
            "Older-dated conflicting versions preserved under _FileForge_Conflicts during Copy. " +
            "Same relative path and same file size, but different SHA256 content hashes.";

        return group;
    }

    private static SourceFileRecord SelectFirstSourceCandidate(
        List<SourceFileRecord> files,
        IReadOnlyList<string> sourceRootOrder)
    {
        return files
            .OrderBy(f => SourceOrderIndex(f.SourceRoot, sourceRootOrder))
            .ThenByDescending(f => f.LastModifiedTime)
            .ThenByDescending(f => f.SizeBytes)
            .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static SourceFileRecord SelectLatestModifiedCandidate(
        List<SourceFileRecord> files,
        IReadOnlyList<string> sourceRootOrder)
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
