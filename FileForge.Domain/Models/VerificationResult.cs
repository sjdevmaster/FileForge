using System;


namespace FileForge.Domain.Models;
public class Class1
{
	public Class1()
	{
	}
}


public sealed class VerificationResult
{
    public string RelativePath { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public string SourceHash { get; set; } = string.Empty;

    public string DestinationHash { get; set; } = string.Empty;

    public long SourceSizeBytes { get; set; }

    public long DestinationSizeBytes { get; set; }

    public bool IsVerified { get; set; }

    public string Message { get; set; } = string.Empty;
}