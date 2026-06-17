using FileForge.Domain.Models;
using FileForge.Infrastructure.Hashing;

namespace FileForge.Application.Services;

public sealed class CopyVerificationService
{
    private readonly Sha256FileHasher _hasher = new();

    public List<CopyVerificationResult> VerifyCopiedFiles(
        IEnumerable<ConsolidationGroup> groups,
        string destinationRoot)
    {
        List<CopyVerificationResult> results = new();

        foreach (ConsolidationGroup group in groups.OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            if (group.SelectedFile == null)
                continue;

            string destinationPath = Path.Combine(destinationRoot, group.RelativePath);

            CopyVerificationResult result = new()
            {
                RelativePath = group.RelativePath,
                SourcePath = group.SelectedFile.FullPath,
                DestinationPath = destinationPath,
                ExpectedSizeBytes = group.SelectedFile.SizeBytes,
                ExpectedHash = group.SelectedFile.Sha256Hash
            };

            try
            {
                if (!File.Exists(group.SelectedFile.FullPath))
                {
                    result.Status = CopyVerificationStatus.SourceMissing;
                    result.Message = "Selected source file is missing.";
                    results.Add(result);
                    continue;
                }

                if (!File.Exists(destinationPath))
                {
                    result.Status = CopyVerificationStatus.Missing;
                    result.Message = "Destination file is missing.";
                    results.Add(result);
                    continue;
                }

                FileInfo destinationInfo = new(destinationPath);
                result.ActualSizeBytes = destinationInfo.Length;

                if (destinationInfo.Length != group.SelectedFile.SizeBytes)
                {
                    result.Status = CopyVerificationStatus.SizeMismatch;
                    result.Message = "Destination file size does not match selected source file.";
                    results.Add(result);
                    continue;
                }

                string sourceHash = group.SelectedFile.HashCalculated && !string.IsNullOrWhiteSpace(group.SelectedFile.Sha256Hash)
                    ? group.SelectedFile.Sha256Hash
                    : _hasher.ComputeHash(group.SelectedFile.FullPath);

                string destinationHash = _hasher.ComputeHash(destinationPath);

                result.ExpectedHash = sourceHash;
                result.ActualHash = destinationHash;

                if (!string.Equals(sourceHash, destinationHash, StringComparison.OrdinalIgnoreCase))
                {
                    result.Status = CopyVerificationStatus.HashMismatch;
                    result.Message = "Destination file hash does not match selected source file.";
                    results.Add(result);
                    continue;
                }

                result.Status = CopyVerificationStatus.Verified;
                result.Message = "Copy verified successfully.";
                results.Add(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Status = CopyVerificationStatus.AccessDenied;
                result.Message = ex.Message;
                results.Add(result);
            }
            catch (IOException ex)
            {
                result.Status = CopyVerificationStatus.IOError;
                result.Message = ex.Message;
                results.Add(result);
            }
            catch (Exception ex)
            {
                result.Status = CopyVerificationStatus.Error;
                result.Message = ex.Message;
                results.Add(result);
            }
        }

        return results;
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
