namespace FileForge.Application.Services;

public sealed class TargetPreflightService
{
    public TargetPreflightResult ValidateNewArchiveTarget(
        IEnumerable<string> sourceRoots,
        string targetRoot)
    {
        List<string> errors = new();
        List<string> normalizedSources = sourceRoots
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizeDirectoryPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedSources.Count == 0)
        {
            errors.Add("At least one source root folder must be selected.");
        }

        if (string.IsNullOrWhiteSpace(targetRoot))
        {
            errors.Add("Target archive folder is required.");
            return TargetPreflightResult.Failed(errors);
        }

        string normalizedTarget = NormalizeDirectoryPath(targetRoot);

        if (!Directory.Exists(normalizedTarget))
        {
            errors.Add("Target archive folder does not exist. Please create/select an empty target folder.");
            return TargetPreflightResult.Failed(errors);
        }

        foreach (string sourceRoot in normalizedSources)
        {
            if (!Directory.Exists(sourceRoot))
            {
                errors.Add($"Source root folder does not exist: {sourceRoot}");
                continue;
            }

            if (PathsEqual(sourceRoot, normalizedTarget))
            {
                errors.Add($"Target folder cannot be the same as a source root: {sourceRoot}");
                continue;
            }

            if (IsChildPath(parentPath: sourceRoot, childPath: normalizedTarget))
            {
                errors.Add($"Target folder cannot be inside a source root. Source: {sourceRoot} | Target: {normalizedTarget}");
            }

            if (IsChildPath(parentPath: normalizedTarget, childPath: sourceRoot))
            {
                errors.Add($"Source root cannot be inside the target archive folder. Source: {sourceRoot} | Target: {normalizedTarget}");
            }
        }

        if (Directory.EnumerateFileSystemEntries(normalizedTarget).Any())
        {
            errors.Add("Target archive folder is not empty. FileForge V1 creates a clean new archive only.");
        }

        return errors.Count == 0
            ? TargetPreflightResult.Success(normalizedTarget)
            : TargetPreflightResult.Failed(errors, normalizedTarget);
    }

    public static string NormalizeDirectoryPath(string path)
    {
        string fullPath = Path.GetFullPath(path.Trim());
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            NormalizeDirectoryPath(left),
            NormalizeDirectoryPath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChildPath(string parentPath, string childPath)
    {
        string parent = NormalizeDirectoryPath(parentPath);
        string child = NormalizeDirectoryPath(childPath);

        if (PathsEqual(parent, child))
            return false;

        string parentWithSeparator = parent + Path.DirectorySeparatorChar;
        return child.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class TargetPreflightResult
{
    public bool IsValid { get; init; }

    public string NormalizedTargetRoot { get; init; } = string.Empty;

    public List<string> Errors { get; init; } = new();

    public string Message => Errors.Count == 0
        ? "Target archive folder passed safety checks."
        : string.Join(Environment.NewLine, Errors);

    public static TargetPreflightResult Success(string normalizedTargetRoot)
    {
        return new TargetPreflightResult
        {
            IsValid = true,
            NormalizedTargetRoot = normalizedTargetRoot
        };
    }

    public static TargetPreflightResult Failed(IEnumerable<string> errors, string normalizedTargetRoot = "")
    {
        return new TargetPreflightResult
        {
            IsValid = false,
            NormalizedTargetRoot = normalizedTargetRoot,
            Errors = errors.ToList()
        };
    }
}
