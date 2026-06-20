using FileForge.Domain.Models;
using System.IO;
using System.Net;
using System.Text;

namespace FileForge.Application.Services;

public sealed class ReportService
{
    public AuditReportResult GenerateHtmlReport(AuditReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetRoot))
            throw new ArgumentException("Target root folder is required for report generation.", nameof(request));

        if (!Directory.Exists(request.TargetRoot))
            throw new DirectoryNotFoundException($"Target root folder does not exist: {request.TargetRoot}");

        string reportFolder = Path.Combine(request.TargetRoot, "FileForge_Report");
        Directory.CreateDirectory(reportFolder);

        string timestamp = request.GeneratedAt.ToString("yyyyMMdd_HHmmss");
        string reportFile = Path.Combine(reportFolder, $"FileForge_Audit_Report_{timestamp}.html");

        string html = BuildHtml(request);
        File.WriteAllText(reportFile, html, Encoding.UTF8);

        return new AuditReportResult
        {
            HtmlReportPath = reportFile,
            ReportFolder = reportFolder
        };
    }

    private static string BuildHtml(AuditReportRequest request)
    {
        int copied = request.CopyRecords.Count(r => r.Success && !r.Skipped && !r.IsConflictVaultCopy);
        int conflictVaultCopied = request.CopyRecords.Count(r => r.Success && r.IsConflictVaultCopy);
        int copyFailed = request.CopyRecords.Count(r => !r.Success && !r.Skipped);
        int copySkipped = request.CopyRecords.Count(r => r.Skipped);
        int verified = request.VerificationResults.Count(r => r.IsVerified);
        int verificationFailed = request.VerificationResults.Count(r => !r.IsVerified);
        bool verificationPerformed = request.VerificationResults.Count > 0;

        long mainArchiveBytes = request.CopyRecords
            .Where(r => r.Success && !r.Skipped && !r.IsConflictVaultCopy)
            .Sum(r => r.BytesCopied);

        long conflictVaultBytes = request.CopyRecords
            .Where(r => r.Success && r.IsConflictVaultCopy)
            .Sum(r => r.BytesCopied);

        long totalPreservedBytes = mainArchiveBytes + conflictVaultBytes;
        long storageSavedBytes = CalculateStorageSavedBytes(request);
        ArchiveHealthSummary archiveHealth = BuildArchiveHealthSummary(
            request,
            copyFailed,
            verificationFailed,
            verified,
            verificationPerformed,
            storageSavedBytes);

        StringBuilder sb = new();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine("<title>FileForge Audit Report</title>");
        sb.AppendLine(BuildStyle());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<main class=\"page\">");
        sb.AppendLine("<section class=\"hero\">");
        sb.AppendLine("<div>");
        sb.AppendLine("<h1>FileForge Audit Report</h1>");
        sb.AppendLine("<p>Consolidate • Deduplicate • Verify • Archive</p>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div class=\"stamp\">Generated<br><strong>{Html(request.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"))}</strong></div>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"summary-grid\" aria-label=\"Report navigation summary\">");
        AddMetric(sb, "Sources", request.SourceRoots.Count, "#sources-section");
        AddMetric(sb, "Total Files", request.TotalFiles, "#archive-decisions-section");
        AddMetric(sb, "To Archive", request.ToArchiveFiles, "#archive-decisions-section");
        AddMetric(sb, "Dup. Skipped", request.DuplicateFilesSkipped, "#decision-summary-section");
        AddMetric(sb, "Storage Saved", FormatBytes(storageSavedBytes), "#storage-summary-section");
        AddMetric(sb, "Conflicts", request.ConflictGroups, "#conflicts-section");
        AddMetric(sb, "Verified", verified, "#verification-summary-section");
        sb.AppendLine("</section>");

        AddArchiveHealthSection(sb, archiveHealth);

        sb.AppendLine("<section class=\"card\" id=\"archive-context-section\">");
        sb.AppendLine("<h2>Archive Context</h2>");
        sb.AppendLine("<table class=\"kv\">");
        AddKv(sb, "Application", request.ApplicationName);
        AddKv(sb, "Application Mode", request.ApplicationMode);
        AddKv(sb, "Hash Algorithm", request.HashAlgorithm);
        AddKv(sb, "Preserve Empty Directories", request.PreserveEmptyDirectories ? "Yes" : "No");
        AddKv(sb, "Target Folder", request.TargetRoot);
        AddKv(sb, "Target Safety Policy", request.TargetSafetyPolicy);
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\" id=\"sources-section\">");
        sb.AppendLine("<h2>Selected Source Roots</h2>");
        sb.AppendLine("<ol class=\"paths\">");
        foreach (string sourceRoot in request.SourceRoots)
            sb.AppendLine($"<li>{Html(sourceRoot)}</li>");
        sb.AppendLine("</ol>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\" id=\"decision-summary-section\">");
        sb.AppendLine("<h2>Decision Summary</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Decision</th><th class=\"num\">Groups</th><th>Meaning</th></tr></thead>");
        sb.AppendLine("<tbody>");
        AddDecisionRow(sb, "Unique", request.UniqueGroups, "Only one file exists at the relative path. It is selected for archive.");
        AddDecisionRow(sb, "Duplicate Same Content", request.DuplicateGroups, "Multiple files share the same relative path and same content. One winner is archived; duplicates are skipped.");
        AddDecisionRow(sb, "Conflict Auto-Resolved", request.ConflictGroups, "Same relative path with different content. Latest modified version is copied to the main archive; older-dated conflicting versions are preserved under _FileForge_Conflicts.");
        AddDecisionRow(sb, "Error", request.Groups.Count(g => g.Status == ConsolidationStatus.Error), "Unreadable files, hashing errors, or other issues. These are skipped and require review.");
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\" id=\"copy-summary-section\">");
        sb.AppendLine("<h2>Copy Summary</h2>");
        sb.AppendLine("<div class=\"mini-grid\">");
        AddMiniMetric(sb, "Main Archive", copied);
        AddMiniMetric(sb, "Conflict Vault", conflictVaultCopied);
        AddMiniMetric(sb, "Failed", copyFailed);
        AddMiniMetric(sb, "Skipped", copySkipped);
        sb.AppendLine("</div>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\" id=\"storage-summary-section\">");
        sb.AppendLine("<h2>Storage Summary</h2>");
        sb.AppendLine("<div class=\"mini-grid\">");
        AddMiniMetric(sb, "Main Archive Size", FormatBytes(mainArchiveBytes));
        AddMiniMetric(sb, "Conflict Vault Size", FormatBytes(conflictVaultBytes));
        AddMiniMetric(sb, "Total Preserved Size", FormatBytes(totalPreservedBytes));
        AddMiniMetric(sb, "Storage Saved", FormatBytes(storageSavedBytes));
        sb.AppendLine("</div>");
        sb.AppendLine("<p class=\"note\">Storage Saved means duplicate same-content bytes that FileForge avoided copying into the main archive. No source files are deleted.</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\" id=\"verification-summary-section\">");
        sb.AppendLine("<h2>Verification Summary</h2>");
        if (!verificationPerformed)
        {
            sb.AppendLine("<p class=\"warning\">Verification was not performed before this report was generated.</p>");
        }
        else
        {
            sb.AppendLine("<div class=\"mini-grid\">");
            AddMiniMetric(sb, "Verified", verified);
            AddMiniMetric(sb, "Failed", verificationFailed);
            AddMiniMetric(sb, "Expected", request.ToArchiveFiles);
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</section>");

        AddConflictSection(sb, request);
        AddFailedCopySection(sb, request);
        AddFailedVerificationSection(sb, request);
        AddArchiveDecisionSection(sb, request);

        sb.AppendLine("<section class=\"footer\">");
        sb.AppendLine("<p>FileForge V1 New Archive Mode: target must be empty before Copy and existing target files are never overwritten automatically.</p>");
        sb.AppendLine("</section>");

        sb.AppendLine("</main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string BuildStyle()
    {
        return """
<style>
:root {
    --bg: #f2f5f9;
    --card: #ffffff;
    --ink: #162033;
    --muted: #647086;
    --blue: #2260b2;
    --green: #008452;
    --red: #b22d2d;
    --amber: #9b6500;
    --line: #d7dde8;
}
* { box-sizing: border-box; }
html { scroll-behavior: smooth; }
body {
    margin: 0;
    background: var(--bg);
    color: var(--ink);
    font-family: "Segoe UI", Arial, sans-serif;
    font-size: 14px;
}
.page {
    max-width: 1180px;
    margin: 26px auto;
    padding: 0 18px 36px;
}
.hero {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 18px;
    background: linear-gradient(135deg, #082c56, #2260b2);
    color: #fff;
    border-radius: 16px;
    padding: 24px 28px;
    box-shadow: 0 10px 24px rgba(8, 44, 86, 0.18);
}
.hero h1 { margin: 0; font-size: 30px; letter-spacing: .2px; }
.hero p { margin: 6px 0 0; color: #dce8f7; }
.stamp { text-align: right; color: #dce8f7; min-width: 220px; }
.stamp strong { color: #fff; }
.summary-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 12px;
    margin: 18px 0;
}
.metric, .mini-metric {
    background: var(--card);
    border: 1px solid var(--line);
    border-radius: 12px;
    padding: 14px 16px;
    box-shadow: 0 3px 10px rgba(20, 35, 60, .05);
}
.metric .label, .mini-metric .label { color: var(--muted); font-size: 12px; }
.metric .value { font-size: 24px; font-weight: 700; margin-top: 4px; }
.metric-link {
    display: block;
    color: inherit;
    text-decoration: none;
    cursor: pointer;
    transition: border-color .15s ease, transform .15s ease;
}
.metric-link:hover { border-color: var(--blue); transform: translateY(-1px); }
.metric-link:focus { outline: 2px solid var(--blue); outline-offset: 2px; }
.metric-link .label::after { content: "  ↴"; color: var(--blue); font-weight: 700; }
.mini-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
}
.mini-metric .value { font-size: 20px; font-weight: 700; margin-top: 4px; }
.card {
    background: var(--card);
    border: 1px solid var(--line);
    border-radius: 14px;
    margin: 16px 0;
    padding: 18px 20px;
    box-shadow: 0 3px 10px rgba(20, 35, 60, .05);
}
h2 { margin: 0 0 12px; font-size: 18px; color: #082c56; }
table { width: 100%; border-collapse: collapse; table-layout: fixed; }
th, td {
    border-bottom: 1px solid var(--line);
    padding: 9px 8px;
    vertical-align: top;
    overflow-wrap: anywhere;
    word-break: break-word;
}
.paths li, .kv td, td { overflow-wrap: anywhere; word-break: break-word; }
th { text-align: left; color: #33415c; font-size: 12px; text-transform: uppercase; letter-spacing: .04em; }
tr:last-child td { border-bottom: none; }
.kv td:first-child { width: 230px; color: var(--muted); font-weight: 600; }
.num { text-align: right; }
.paths { margin: 0; padding-left: 22px; }
.paths li { margin: 6px 0; }
.badge { display: inline-block; padding: 3px 8px; border-radius: 999px; font-size: 12px; font-weight: 600; }
.good { color: var(--green); }
.bad { color: var(--red); }
.warn { color: var(--amber); }
.warning { color: var(--amber); font-weight: 600; }
.health-banner {
    border: 1px solid var(--line);
    border-left: 6px solid var(--blue);
    border-radius: 12px;
    padding: 14px 16px;
    background: #f8fbff;
    margin-bottom: 14px;
}
.health-banner.good { border-left-color: var(--green); color: var(--ink); }
.health-banner.warn { border-left-color: var(--amber); color: var(--ink); }
.health-banner.bad { border-left-color: var(--red); color: var(--ink); }
.health-title { font-size: 22px; font-weight: 800; margin: 0 0 6px; }
.health-message { margin: 0; color: var(--muted); }
.note { color: var(--muted); margin: 12px 0 0; }
.footer { color: var(--muted); font-size: 12px; text-align: center; padding: 18px; }
@media print {
    body { background: #fff; }
    .page { margin: 0; max-width: none; }
    .card, .metric, .hero { box-shadow: none; }
}
@media (max-width: 900px) {
    .summary-grid { grid-template-columns: repeat(2, 1fr); }
    .mini-grid { grid-template-columns: 1fr; }
    .hero { align-items: flex-start; flex-direction: column; }
    .stamp { text-align: left; }
}
</style>
""";
    }


    private static ArchiveHealthSummary BuildArchiveHealthSummary(
        AuditReportRequest request,
        int copyFailed,
        int verificationFailed,
        int verified,
        bool verificationPerformed,
        long storageSavedBytes)
    {
        if (copyFailed > 0)
        {
            return new ArchiveHealthSummary
            {
                Verdict = "Action Required — Copy Failures",
                CssClass = "bad",
                Message = "One or more files failed during copy. Review copy failures before trusting this archive.",
                VerifiedText = verificationPerformed ? $"{verified:N0} / {request.ToArchiveFiles:N0}" : "Not verified",
                FailureCount = copyFailed + verificationFailed,
                ConflictCount = request.ConflictGroups,
                DuplicateSkippedCount = request.DuplicateFilesSkipped,
                StorageSavedText = FormatBytes(storageSavedBytes)
            };
        }

        if (verificationPerformed && verificationFailed > 0)
        {
            return new ArchiveHealthSummary
            {
                Verdict = "Action Required — Verification Failed",
                CssClass = "bad",
                Message = "Some archived files failed verification. The archive should not be treated as clean until failures are resolved.",
                VerifiedText = $"{verified:N0} / {request.ToArchiveFiles:N0}",
                FailureCount = verificationFailed,
                ConflictCount = request.ConflictGroups,
                DuplicateSkippedCount = request.DuplicateFilesSkipped,
                StorageSavedText = FormatBytes(storageSavedBytes)
            };
        }

        if (!verificationPerformed)
        {
            return new ArchiveHealthSummary
            {
                Verdict = "Warning — Not Verified",
                CssClass = "warn",
                Message = "The archive report was generated before verification. Run Verify before treating this archive as complete.",
                VerifiedText = "Not verified",
                FailureCount = copyFailed,
                ConflictCount = request.ConflictGroups,
                DuplicateSkippedCount = request.DuplicateFilesSkipped,
                StorageSavedText = FormatBytes(storageSavedBytes)
            };
        }

        if (verified != request.ToArchiveFiles)
        {
            return new ArchiveHealthSummary
            {
                Verdict = "Warning — Verification Incomplete",
                CssClass = "warn",
                Message = "Verification did not cover every file expected in the main archive. Review verification details.",
                VerifiedText = $"{verified:N0} / {request.ToArchiveFiles:N0}",
                FailureCount = verificationFailed,
                ConflictCount = request.ConflictGroups,
                DuplicateSkippedCount = request.DuplicateFilesSkipped,
                StorageSavedText = FormatBytes(storageSavedBytes)
            };
        }

        string verdict;
        string message;

        if (request.ConflictGroups > 0 && request.DuplicateFilesSkipped > 0)
        {
            verdict = "Clean — Verified with Conflicts Preserved";
            message = "All main archive files verified successfully. Same-content duplicates were skipped, and older conflicting versions were preserved in the conflict vault.";
        }
        else if (request.ConflictGroups > 0)
        {
            verdict = "Clean — Verified with Conflicts Preserved";
            message = "All main archive files verified successfully. Older conflicting versions were preserved in the conflict vault.";
        }
        else if (request.DuplicateFilesSkipped > 0)
        {
            verdict = "Clean — Verified with Duplicates Skipped";
            message = "All main archive files verified successfully. Same-content duplicate files were skipped from the archive copy.";
        }
        else
        {
            verdict = "Clean — Verified";
            message = "All selected archive files copied and verified successfully.";
        }

        return new ArchiveHealthSummary
        {
            Verdict = verdict,
            CssClass = "good",
            Message = message,
            VerifiedText = $"{verified:N0} / {request.ToArchiveFiles:N0}",
            FailureCount = 0,
            ConflictCount = request.ConflictGroups,
            DuplicateSkippedCount = request.DuplicateFilesSkipped,
            StorageSavedText = FormatBytes(storageSavedBytes)
        };
    }

    private static void AddArchiveHealthSection(StringBuilder sb, ArchiveHealthSummary health)
    {
        sb.AppendLine("<section class=\"card\" id=\"archive-health-section\">");
        sb.AppendLine("<h2>Archive Health</h2>");
        sb.AppendLine($"<div class=\"health-banner {Html(health.CssClass)}\">");
        sb.AppendLine($"<p class=\"health-title\">{Html(health.Verdict)}</p>");
        sb.AppendLine($"<p class=\"health-message\">{Html(health.Message)}</p>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"mini-grid\">");
        AddMiniMetric(sb, "Verified", health.VerifiedText);
        AddMiniMetric(sb, "Failures", health.FailureCount);
        AddMiniMetric(sb, "Conflicts Preserved", health.ConflictCount);
        AddMiniMetric(sb, "Duplicates Skipped", health.DuplicateSkippedCount);
        AddMiniMetric(sb, "Storage Saved", health.StorageSavedText);
        sb.AppendLine("</div>");
        sb.AppendLine("<p class=\"note\">Archive Health is a summary verdict based on copy results, verification results, duplicate decisions, conflict preservation, and storage saved. It does not replace the detailed audit tables below.</p>");
        sb.AppendLine("</section>");
    }

    private static long CalculateStorageSavedBytes(AuditReportRequest request)
    {
        long storageSavedBytes = 0;

        foreach (ConsolidationGroup group in request.Groups)
        {
            if (group.Status != ConsolidationStatus.DuplicateSameContent || group.SelectedFile == null)
                continue;

            string selectedPath = group.SelectedFile.FullPath;

            storageSavedBytes += group.Files
                .Where(f => !string.Equals(f.FullPath, selectedPath, StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.SizeBytes);
        }

        return storageSavedBytes;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            bytes = 0;

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        string formatted = unitIndex == 0
            ? value.ToString("N0")
            : value.ToString("N1");

        return $"{formatted} {units[unitIndex]}";
    }

    private static void AddMetric(StringBuilder sb, string label, int value, string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            sb.AppendLine("<div class=\"metric\">");
            sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
            sb.AppendLine($"<div class=\"value\">{value:N0}</div>");
            sb.AppendLine("</div>");
            return;
        }

        sb.AppendLine($"<a class=\"metric metric-link\" href=\"{Html(href)}\" title=\"Jump to {Html(label)} section\">");
        sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{value:N0}</div>");
        sb.AppendLine("</a>");
    }

    private static void AddMetric(StringBuilder sb, string label, string value, string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            sb.AppendLine("<div class=\"metric\">");
            sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
            sb.AppendLine($"<div class=\"value\">{Html(value)}</div>");
            sb.AppendLine("</div>");
            return;
        }

        sb.AppendLine($"<a class=\"metric metric-link\" href=\"{Html(href)}\" title=\"Jump to {Html(label)} section\">");
        sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{Html(value)}</div>");
        sb.AppendLine("</a>");
    }

    private static void AddMiniMetric(StringBuilder sb, string label, int value)
    {
        sb.AppendLine("<div class=\"mini-metric\">");
        sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{value:N0}</div>");
        sb.AppendLine("</div>");
    }

    private static void AddMiniMetric(StringBuilder sb, string label, string value)
    {
        sb.AppendLine("<div class=\"mini-metric\">");
        sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{Html(value)}</div>");
        sb.AppendLine("</div>");
    }

    private static void AddKv(StringBuilder sb, string key, string value)
    {
        sb.AppendLine($"<tr><td>{Html(key)}</td><td>{Html(value)}</td></tr>");
    }

    private static void AddDecisionRow(StringBuilder sb, string decision, int count, string meaning)
    {
        sb.AppendLine($"<tr><td>{Html(decision)}</td><td class=\"num\">{count:N0}</td><td>{Html(meaning)}</td></tr>");
    }

    private static void AddConflictSection(StringBuilder sb, AuditReportRequest request)
    {
        List<ConsolidationGroup> conflicts = request.Groups
            .Where(g => g.Status == ConsolidationStatus.ConflictDifferentContent)
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        sb.AppendLine("<section class=\"card\" id=\"conflicts-section\">");
        sb.AppendLine("<h2>Auto-Resolved Conflicts</h2>");

        if (conflicts.Count == 0)
        {
            sb.AppendLine("<p>No auto-resolved conflicts were found in this archive run.</p>");
            sb.AppendLine("</section>");
            AddConflictVaultSection(sb, request);
            AddErrorSection(sb, request);
            return;
        }

        sb.AppendLine("<p class=\"warning\">Conflict auto-resolved. Main archive version selected by latest modified date. Older-dated conflicting versions preserved under _FileForge_Conflicts.</p>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Main Archive Source</th><th>Resolution</th><th class=\"num\">Older Versions</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (ConsolidationGroup group in conflicts)
        {
            SourceFileRecord? selected = group.SelectedFile;
            int olderVersions = selected == null
                ? 0
                : group.Files.Count(f => !string.Equals(f.FullPath, selected.FullPath, StringComparison.OrdinalIgnoreCase));

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{Html(group.RelativePath)}</td>");
            sb.AppendLine($"<td>{Html(selected == null ? string.Empty : request.IncludeFullSourcePaths ? selected.FullPath : selected.SourceRoot)}</td>");
            sb.AppendLine("<td>Latest modified version copied to the main archive. Older-dated conflicting versions preserved in the conflict vault.</td>");
            sb.AppendLine($"<td class=\"num\">{olderVersions:N0}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        AddConflictVaultSection(sb, request);
        AddErrorSection(sb, request);
    }

    private static void AddConflictVaultSection(StringBuilder sb, AuditReportRequest request)
    {
        List<AuditCopyRecord> vaultRecords = request.CopyRecords
            .Where(r => r.IsConflictVaultCopy)
            .OrderBy(r => r.OriginalRelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (vaultRecords.Count == 0)
            return;

        sb.AppendLine("<section class=\"card\" id=\"conflict-vault-section\">");
        sb.AppendLine("<h2>Conflict Vault Copies</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Original Relative Path</th><th>Vault Relative Path</th><th>Source</th><th>Target</th><th>Status</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (AuditCopyRecord record in vaultRecords)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{Html(record.OriginalRelativePath)}</td>");
            sb.AppendLine($"<td>{Html(record.RelativePath)}</td>");
            sb.AppendLine($"<td>{Html(request.IncludeFullSourcePaths ? record.SourcePath : Path.GetFileName(record.SourcePath))}</td>");
            sb.AppendLine($"<td>{Html(record.DestinationPath)}</td>");
            string statusClass = record.Success ? "good" : "bad";
            string statusText = record.Success ? "Preserved" : "Failed";
            sb.AppendLine($"<td class=\"{statusClass}\">{Html(statusText)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");
    }

    private static void AddErrorSection(StringBuilder sb, AuditReportRequest request)
    {
        List<ConsolidationGroup> errors = request.Groups
            .Where(g => g.Status == ConsolidationStatus.Error)
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (errors.Count == 0)
            return;

        sb.AppendLine("<section class=\"card\" id=\"errors-section\">");
        sb.AppendLine("<h2>Errors Requiring Review</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Reason</th><th class=\"num\">Candidates</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (ConsolidationGroup group in errors)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{Html(group.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"bad\">{Html(group.DecisionReason)}</td>");
            sb.AppendLine($"<td class=\"num\">{group.Files.Count:N0}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");
    }

    private static void AddFailedCopySection(StringBuilder sb, AuditReportRequest request)
    {
        List<AuditCopyRecord> failed = request.CopyRecords
            .Where(r => !r.Success && !r.Skipped)
            .OrderBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (failed.Count == 0)
            return;

        sb.AppendLine("<section class=\"card\" id=\"copy-failures-section\">");
        sb.AppendLine("<h2>Copy Failures</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Reason</th><th>Source</th><th>Target</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (AuditCopyRecord record in failed)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{Html(record.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"bad\">{Html(record.Message)}</td>");
            sb.AppendLine($"<td>{Html(request.IncludeFullSourcePaths ? record.SourcePath : Path.GetFileName(record.SourcePath))}</td>");
            sb.AppendLine($"<td>{Html(record.DestinationPath)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");
    }

    private static void AddFailedVerificationSection(StringBuilder sb, AuditReportRequest request)
    {
        List<CopyVerificationResult> failed = request.VerificationResults
            .Where(r => !r.IsVerified)
            .OrderBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (failed.Count == 0)
            return;

        sb.AppendLine("<section class=\"card\" id=\"verification-failures-section\">");
        sb.AppendLine("<h2>Verification Failures</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Failure</th><th>Message</th><th>Source</th><th>Target</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (CopyVerificationResult record in failed)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{Html(record.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"bad\">{Html(record.Status.ToString())}</td>");
            sb.AppendLine($"<td>{Html(record.Message)}</td>");
            sb.AppendLine($"<td>{Html(request.IncludeFullSourcePaths ? record.SourcePath : Path.GetFileName(record.SourcePath))}</td>");
            sb.AppendLine($"<td>{Html(record.DestinationPath)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");
    }

    private static void AddArchiveDecisionSection(StringBuilder sb, AuditReportRequest request)
    {
        List<ConsolidationGroup> selectedGroups = request.Groups
            .Where(g => g.SelectedFile != null &&
                        (g.Status == ConsolidationStatus.Unique ||
                         g.Status == ConsolidationStatus.DuplicateSameContent ||
                         g.Status == ConsolidationStatus.ConflictDifferentContent))
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        sb.AppendLine("<section class=\"card\" id=\"archive-decisions-section\">");
        sb.AppendLine("<h2>Archive Decisions</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Decision</th><th>Selected Source</th><th class=\"num\">Candidates</th><th class=\"num\">Size</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (ConsolidationGroup group in selectedGroups)
        {
            SourceFileRecord selected = group.SelectedFile!;
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{Html(group.RelativePath)}</td>");
            sb.AppendLine($"<td>{Html(FormatStatus(group.Status))}</td>");
            sb.AppendLine($"<td>{Html(request.IncludeFullSourcePaths ? selected.FullPath : selected.SourceRoot)}</td>");
            sb.AppendLine($"<td class=\"num\">{group.Files.Count:N0}</td>");
            sb.AppendLine($"<td class=\"num\">{selected.SizeBytes:N0}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");
    }

    private static string FormatStatus(ConsolidationStatus status)
    {
        return status switch
        {
            ConsolidationStatus.Unique => "Unique",
            ConsolidationStatus.DuplicateSameContent => "Duplicate Same Content",
            ConsolidationStatus.ConflictDifferentContent => "Conflict Different Content",
            ConsolidationStatus.Error => "Error",
            _ => "Unknown"
        };
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}


internal sealed class ArchiveHealthSummary
{
    public string Verdict { get; set; } = string.Empty;

    public string CssClass { get; set; } = "good";

    public string Message { get; set; } = string.Empty;

    public string VerifiedText { get; set; } = string.Empty;

    public int FailureCount { get; set; }

    public int ConflictCount { get; set; }

    public int DuplicateSkippedCount { get; set; }

    public string StorageSavedText { get; set; } = string.Empty;
}

public sealed class AuditReportRequest
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public string ApplicationName { get; set; } = "FileForge";

    public string ApplicationMode { get; set; } = "New Archive Mode";

    public string HashAlgorithm { get; set; } = "SHA256";

    public string TargetSafetyPolicy { get; set; } = string.Empty;

    public List<string> SourceRoots { get; set; } = new();

    public string TargetRoot { get; set; } = string.Empty;

    public bool PreserveEmptyDirectories { get; set; }

    public int TotalFiles { get; set; }

    public int UniqueGroups { get; set; }

    public int DuplicateGroups { get; set; }

    public int ToArchiveFiles { get; set; }

    public int DuplicateFilesSkipped { get; set; }

    public int ConflictGroups { get; set; }

    public List<SourceFileRecord> ScannedFiles { get; set; } = new();

    public List<ConsolidationGroup> Groups { get; set; } = new();

    public List<AuditCopyRecord> CopyRecords { get; set; } = new();

    public List<CopyVerificationResult> VerificationResults { get; set; } = new();

    public bool IncludeFullSourcePaths { get; set; } = true;
}

public sealed class AuditCopyRecord
{
    public string RelativePath { get; set; } = string.Empty;

    public string OriginalRelativePath { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public bool Success { get; set; }

    public bool Skipped { get; set; }

    public bool IsConflictVaultCopy { get; set; }

    public string CopyRole { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public long BytesCopied { get; set; }
}

public sealed class AuditReportResult
{
    public string HtmlReportPath { get; set; } = string.Empty;

    public string ReportFolder { get; set; } = string.Empty;
}
