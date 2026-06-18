using FileForge.Domain.Models;
using FileForge.Infrastructure.Hashing;
using System.Collections.Concurrent;

namespace FileForge.Application.Services;

public sealed class CopyVerificationService
{
    private readonly Sha256FileHasher _hasher = new();

    public List<CopyVerificationResult> VerifyCopiedFiles(
        IEnumerable<ConsolidationGroup> groups,
        string destinationRoot)
    {
        return VerifyCopiedFilesInternal(groups, destinationRoot, null, CancellationToken.None);
    }

    public Task<List<CopyVerificationResult>> VerifyCopiedFilesAsync(
        IEnumerable<ConsolidationGroup> groups,
        string destinationRoot,
        IProgress<CopyVerificationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<ConsolidationGroup> expectedGroups = groups
            .Where(IsExpectedCopiedGroup)
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.Run(() =>
            VerifyCopiedFilesInternal(expectedGroups, destinationRoot, progress, cancellationToken),
            cancellationToken);
    }

    private List<CopyVerificationResult> VerifyCopiedFilesInternal(
        IEnumerable<ConsolidationGroup> groups,
        string destinationRoot,
        IProgress<CopyVerificationProgress>? progress,
        CancellationToken cancellationToken)
    {
        List<ConsolidationGroup> expectedGroups = groups
            .Where(IsExpectedCopiedGroup)
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int total = expectedGroups.Count;

        if (total == 0)
        {
            progress?.Report(new CopyVerificationProgress
            {
                Total = 0,
                Completed = 0,
                CurrentFile = string.Empty
            });

            return new List<CopyVerificationResult>();
        }

        ConcurrentBag<CopyVerificationResult> results = new();
        int completed = 0;

        ParallelOptions options = new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 2))
        };

        Parallel.ForEach(expectedGroups, options, group =>
        {
            options.CancellationToken.ThrowIfCancellationRequested();

            CopyVerificationResult result = VerifyOneGroup(group, destinationRoot);
            results.Add(result);

            int done = Interlocked.Increment(ref completed);

            if (done == 1 || done == total || done % 10 == 0)
            {
                progress?.Report(new CopyVerificationProgress
                {
                    Total = total,
                    Completed = done,
                    CurrentFile = group.RelativePath
                });
            }
        });

        return results
            .OrderBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private CopyVerificationResult VerifyOneGroup(ConsolidationGroup group, string destinationRoot)
    {
        SourceFileRecord selectedFile = group.SelectedFile!;
        string destinationPath = Path.Combine(destinationRoot, group.RelativePath);

        CopyVerificationResult result = new()
        {
            RelativePath = group.RelativePath,
            SourcePath = selectedFile.FullPath,
            DestinationPath = destinationPath,
            ExpectedSizeBytes = selectedFile.SizeBytes,
            ExpectedHash = selectedFile.Sha256Hash
        };

        try
        {
            if (!File.Exists(selectedFile.FullPath))
            {
                result.Status = CopyVerificationStatus.SourceMissing;
                result.Message = "Selected source file is missing.";
                return result;
            }

            if (!File.Exists(destinationPath))
            {
                result.Status = CopyVerificationStatus.Missing;
                result.Message = "Destination file is missing.";
                return result;
            }

            FileInfo destinationInfo = new(destinationPath);
            result.ActualSizeBytes = destinationInfo.Length;

            if (destinationInfo.Length != selectedFile.SizeBytes)
            {
                result.Status = CopyVerificationStatus.SizeMismatch;
                result.Message = "Destination file size does not match selected source file.";
                return result;
            }

            string sourceHash = selectedFile.HashCalculated && !string.IsNullOrWhiteSpace(selectedFile.Sha256Hash)
                ? selectedFile.Sha256Hash
                : _hasher.ComputeHash(selectedFile.FullPath);

            string destinationHash = _hasher.ComputeHash(destinationPath);

            result.ExpectedHash = sourceHash;
            result.ActualHash = destinationHash;

            if (!string.Equals(sourceHash, destinationHash, StringComparison.OrdinalIgnoreCase))
            {
                result.Status = CopyVerificationStatus.HashMismatch;
                result.Message = "Destination file hash does not match selected source file.";
                return result;
            }

            result.Status = CopyVerificationStatus.Verified;
            result.Message = "Copy verified successfully.";
            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            result.Status = CopyVerificationStatus.AccessDenied;
            result.Message = ex.Message;
            return result;
        }
        catch (IOException ex)
        {
            result.Status = CopyVerificationStatus.IOError;
            result.Message = ex.Message;
            return result;
        }
        catch (Exception ex)
        {
            result.Status = CopyVerificationStatus.Error;
            result.Message = ex.Message;
            return result;
        }
    }

    private static bool IsExpectedCopiedGroup(ConsolidationGroup group)
    {
        return group.SelectedFile != null &&
               (group.Status == ConsolidationStatus.Unique ||
                group.Status == ConsolidationStatus.DuplicateSameContent ||
                group.Status == ConsolidationStatus.ConflictDifferentContent);
    }
}

public sealed class CopyVerificationResult
{
    public string RelativePath { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public long ExpectedSizeBytes { get; set; }

    public long ActualSizeBytes { get; set; }

    public string ExpectedHash { get; set; } = string.Empty;

    public string ActualHash { get; set; } = string.Empty;

    public CopyVerificationStatus Status { get; set; } = CopyVerificationStatus.Unknown;

    public string Message { get; set; } = string.Empty;

    public bool IsVerified => Status == CopyVerificationStatus.Verified;
}

public sealed class CopyVerificationProgress
{
    public int Total { get; init; }

    public int Completed { get; init; }

    public string CurrentFile { get; init; } = string.Empty;
}

public enum CopyVerificationStatus
{
    Unknown = 0,
    Verified = 1,
    Missing = 2,
    SizeMismatch = 3,
    HashMismatch = 4,
    SourceMissing = 5,
    AccessDenied = 6,
    IOError = 7,
    Error = 8
}
