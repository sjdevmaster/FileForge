using FileForge.Domain.Models;

namespace FileForge.Application.Services;

public sealed class FolderScanService
{
    public List<SourceFileRecord> ScanFolders(IEnumerable<string> sourceFolders)
    {
        List<SourceFileRecord> results = new();

        foreach (string sourceRoot in sourceFolders)
        {
            if (!Directory.Exists(sourceRoot))
                continue;

            IEnumerable<string> files =
                Directory.EnumerateFiles(
                    sourceRoot,
                    "*.*",
                    SearchOption.AllDirectories);

            foreach (string filePath in files)
            {
                FileInfo fileInfo = new(filePath);

                SourceFileRecord record = new()
                {
                    SourceRoot = sourceRoot,
                    FullPath = filePath,
                    RelativePath = Path.GetRelativePath(sourceRoot, filePath),
                    FileName = fileInfo.Name,
                    DirectoryPath = fileInfo.DirectoryName ?? string.Empty,
                    SizeBytes = fileInfo.Length,
                    CreatedTime = fileInfo.CreationTime,
                    LastModifiedTime = fileInfo.LastWriteTime,
                    HashCalculated = false
                };

                results.Add(record);
            }
        }

        return results;
    }
}