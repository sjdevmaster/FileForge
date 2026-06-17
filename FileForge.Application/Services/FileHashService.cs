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
        List<SourceFileRecord> filesToHash = GetRequiredHashFiles(files);

        foreach (SourceFileRecord file in filesToHash)
        {
            CalculateHash(file);
        }

        return filesToHash.Count;
    }

    public Task<int> CalculateRequiredHashesAsync(
        IEnumerable<SourceFileRecord> files,
        IProgress<FileHashProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<SourceFileRecord> filesToHash = GetRequiredHashFiles(files);
        int total = filesToHash.Count;

        if (total == 0)
        {
            progress?.Report(new FileHashProgress
            {
                Total = 0,
                Completed = 0,
                CurrentFile = string.Empty
            });

            return Task.FromResult(0);
        }

        return Task.Run(() =>
        {
            int completed = 0;

            ParallelOptions options = new()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
            };

            Parallel.ForEach(filesToHash, options, file =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                CalculateHash(file);

                int done = Interlocked.Increment(ref completed);

                if (done == 1 || done == total || done % 10 == 0)
                {
                    progress?.Report(new FileHashProgress
                    {
                        Total = total,
                        Completed = done,
                        CurrentFile = file.RelativePath
                    });
                }
            });

            return total;
        }, cancellationToken);
    }

    private static List<SourceFileRecord> GetRequiredHashFiles(IEnumerable<SourceFileRecord> files)
    {
        return files
            .GroupBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Where(g => g.Select(f => f.SizeBytes).Distinct().Count() == 1)
            .SelectMany(g => g)
            .Where(f => !f.HashCalculated)
            .ToList();
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

public sealed class FileHashProgress
{
    public int Total { get; init; }

    public int Completed { get; init; }

    public string CurrentFile { get; init; } = string.Empty;
}
