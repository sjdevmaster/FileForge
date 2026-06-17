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

        Directory.CreateDirectory(destinationRoot);

        if (preserveEmptyDirectories && sourceRoots != null)
        {
            CreateDirectoryStructure(sourceRoots, destinationRoot);
        }

        foreach (ConsolidationGroup group in groups)
        {
            if (group.SelectedFile == null)
                continue;

            string destinationFile = Path.Combine(destinationRoot, group.RelativePath);
            string? destinationFolder = Path.GetDirectoryName(destinationFile);

            if (!string.IsNullOrWhiteSpace(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            File.Copy(group.SelectedFile.FullPath, destinationFile, true);
        }
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
                Directory.CreateDirectory(destinationDirectory);
            }
        }
    }
}
