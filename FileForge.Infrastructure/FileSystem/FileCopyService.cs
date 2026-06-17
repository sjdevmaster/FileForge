using FileForge.Domain.Models;

namespace FileForge.Infrastructure.FileSystem;

public sealed class FileCopyService
{
    public void CopySelectedFiles(
        IEnumerable<ConsolidationGroup> groups,
        string destinationRoot,
        bool preserveEmptyDirectories = false,
        IEnumerable<string>? sourceRoots = null)
    {
        if (string.IsNullOrWhiteSpace(destinationRoot))
            throw new ArgumentException("Destination root folder is required.", nameof(destinationRoot));

        string normalizedDestinationRoot = NormalizeDirectoryPath(destinationRoot);

        if (!Directory.Exists(normalizedDestinationRoot))
            throw new DirectoryNotFoundException($"Destination root folder does not exist: {normalizedDestinationRoot}");

        ValidateTargetSafety(normalizedDestinationRoot, sourceRoots);

        if (preserveEmptyDirectories && sourceRoots != null)
        {
            CreateDirectoryStructure(sourceRoots, normalizedDestinationRoot);
        }

        foreach (ConsolidationGroup group in groups)
        {
            if (!IsCopyableGroup(group))
                continue;

            SourceFileRecord selectedFile = group.SelectedFile!;
            string destinationFile = Path.Combine(normalizedDestinationRoot, group.RelativePath);
            string? destinationFolder = Path.GetDirectoryName(destinationFile);

            if (!File.Exists(selectedFile.FullPath))
                throw new FileNotFoundException($"Selected source file does not exist: {selectedFile.FullPath}", selectedFile.FullPath);

            if (!string.IsNullOrWhiteSpace(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            if (File.Exists(destinationFile))
            {
                throw new IOException(
                    "Copy blocked by FileForge Target Safety Rule V1. " +
                    $"Target file already exists and will not be overwritten: {destinationFile}");
            }

            File.Copy(selectedFile.FullPath, destinationFile, overwrite: false);
        }
    }

    private static void ValidateTargetSafety(string destinationRoot, IEnumerable<string>? sourceRoots)
    {
        if (Directory.EnumerateFileSystemEntries(destinationRoot).Any())
        {
            throw new IOException(
                "Copy blocked by FileForge Target Safety Rule V1. " +
                "Target folder must be empty before Copy.");
        }

        if (sourceRoots == null)
            return;

        foreach (string sourceRootValue in sourceRoots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            string sourceRoot = NormalizeDirectoryPath(sourceRootValue);

            if (PathsEqual(sourceRoot, destinationRoot))
            {
                throw new IOException(
                    "Copy blocked by FileForge Target Safety Rule V1. " +
                    $"Target folder cannot be the same as a source root: {sourceRoot}");
            }

            if (IsChildPath(parentPath: sourceRoot, childPath: destinationRoot))
            {
                throw new IOException(
                    "Copy blocked by FileForge Target Safety Rule V1. " +
                    $"Target folder cannot be inside a source root. Source: {sourceRoot} | Target: {destinationRoot}");
            }

            if (IsChildPath(parentPath: destinationRoot, childPath: sourceRoot))
            {
                throw new IOException(
                    "Copy blocked by FileForge Target Safety Rule V1. " +
                    $"Source root cannot be inside the target folder. Source: {sourceRoot} | Target: {destinationRoot}");
            }
        }
    }

    private static bool IsCopyableGroup(ConsolidationGroup group)
    {
        return group.SelectedFile != null &&
               (group.Status == ConsolidationStatus.Unique ||
                group.Status == ConsolidationStatus.DuplicateSameContent);
    }

    private static void CreateDirectoryStructure(IEnumerable<string> sourceRoots, string destinationRoot)
    {
        foreach (string sourceRoot in sourceRoots)
        {
            if (!Directory.Exists(sourceRoot))
                continue;

            foreach (string sourceDirectory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relativeDirectory = Path.GetRelativePath(sourceRoot, sourceDirectory);

                if (string.IsNullOrWhiteSpace(relativeDirectory) || relativeDirectory == ".")
                    continue;

                string destinationDirectory = Path.Combine(destinationRoot, relativeDirectory);

                if (File.Exists(destinationDirectory))
                {
                    throw new IOException(
                        "Copy blocked by FileForge Target Safety Rule V1. " +
                        $"A file already exists where a directory is required: {destinationDirectory}");
                }

                Directory.CreateDirectory(destinationDirectory);
            }
        }
    }

    private static string NormalizeDirectoryPath(string path)
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
