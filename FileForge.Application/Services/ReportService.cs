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
        int copied = request.CopyRecords.Count(r => r.Success && !r.Skipped);
        int copyFailed = request.CopyRecords.Count(r => !r.Success && !r.Skipped);
        int copySkipped = request.CopyRecords.Count(r => r.Skipped);
        int verified = request.VerificationResults.Count(r => r.IsVerified);
        int verificationFailed = request.VerificationResults.Count(r => !r.IsVerified);
        bool verificationPerformed = request.VerificationResults.Count > 0;

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

        sb.AppendLine("<section class=\"summary-grid\">");
        AddMetric(sb, "Sources", request.SourceRoots.Count);
        AddMetric(sb, "Total Files", request.TotalFiles);
        AddMetric(sb, "To Archive", request.ToArchiveFiles);
        AddMetric(sb, "Dup. Skipped", request.DuplicateFilesSkipped);
        AddMetric(sb, "Conflicts", request.ConflictGroups);
        AddMetric(sb, "Verified", verified);
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Archive Context</h2>");
        sb.AppendLine("<table class=\"kv data-table\">");
        AddKv(sb, "Application", request.ApplicationName);
        AddKv(sb, "Application Mode", request.ApplicationMode);
        AddKv(sb, "Hash Algorithm", request.HashAlgorithm);
        AddKv(sb, "Preserve Empty Directories", request.PreserveEmptyDirectories ? "Yes" : "No");
        AddKvPath(sb, "Target Folder", request.TargetRoot);
        AddKv(sb, "Target Safety Policy", request.TargetSafetyPolicy);
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Selected Source Roots</h2>");
        sb.AppendLine("<ol class=\"paths\">");
        foreach (string sourceRoot in request.SourceRoots)
            sb.AppendLine($"<li>{PathHtml(sourceRoot)}</li>");
        sb.AppendLine("</ol>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Decision Summary</h2>");
        sb.AppendLine("<table class=\"data-table\">");
        sb.AppendLine("<thead><tr><th>Decision</th><th class=\"num\">Groups</th><th>Meaning</th></tr></thead>");
        sb.AppendLine("<tbody>");
        AddDecisionRow(sb, "Unique", request.UniqueGroups, "Only one file exists at the relative path. It is selected for archive.");
        AddDecisionRow(sb, "Duplicate Same Content", request.DuplicateGroups, "Multiple files share the same relative path and same content. One winner is archived; duplicates are skipped.");
        AddDecisionRow(sb, "Conflict / Error", request.ConflictGroups, "Same relative path with different content, unreadable files, or other issues. Not copied in V1.");
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Copy Summary</h2>");
        sb.AppendLine("<div class=\"mini-grid\">");
        AddMiniMetric(sb, "Copied", copied);
        AddMiniMetric(sb, "Skipped", copySkipped);
        AddMiniMetric(sb, "Failed", copyFailed);
        sb.AppendLine("</div>");
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"card\">");
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
html { overflow-x: hidden; }
body {
    margin: 0;
    background: var(--bg);
    color: var(--ink);
    font-family: "Segoe UI", Arial, sans-serif;
    font-size: 14px;
    line-height: 1.4;
    overflow-x: hidden;
}
.page {
    max-width: 1440px;
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
    grid-template-columns: repeat(6, minmax(0, 1fr));
    gap: 12px;
    margin: 18px 0;
}
.metric, .mini-metric {
    background: var(--card);
    border: 1px solid var(--line);
    border-radius: 12px;
    padding: 14px 16px;
    box-shadow: 0 3px 10px rgba(20, 35, 60, .05);
    min-width: 0;
}
.metric .label, .mini-metric .label { color: var(--muted); font-size: 12px; }
.metric .value { font-size: 24px; font-weight: 700; margin-top: 4px; }
.mini-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
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
    overflow: hidden;
}
h2 { margin: 0 0 12px; font-size: 18px; color: #082c56; }
.data-table {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
}
th, td {
    border-bottom: 1px solid var(--line);
    padding: 9px 8px;
    vertical-align: top;
    overflow-wrap: anywhere;
    word-break: break-word;
}
th {
    text-align: left;
    color: #33415c;
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: .04em;
}
tr:last-child td { border-bottom: none; }
.kv td:first-child { width: 230px; color: var(--muted); font-weight: 600; }
.num {
    text-align: right;
    white-space: nowrap;
    overflow-wrap: normal;
    word-break: normal;
}
.path, .paths li {
    font-family: "Segoe UI", Arial, sans-serif;
    overflow-wrap: anywhere;
    word-break: break-word;
    line-height: 1.35;
}
.paths { margin: 0; padding-left: 22px; }
.paths li { margin: 6px 0; }
.status-cell { max-width: 130px; }
.badge { display: inline-block; padding: 3px 8px; border-radius: 999px; font-size: 12px; font-weight: 600; }
.good { color: var(--green); }
.bad { color: var(--red); }
.warn { color: var(--amber); }
.warning { color: var(--amber); font-weight: 600; }
.footer { color: var(--muted); font-size: 12px; text-align: center; padding: 18px; }
.archive-table col:nth-child(1) { width: 34%; }
.archive-table col:nth-child(2) { width: 13%; }
.archive-table col:nth-child(3) { width: 36%; }
.archive-table col:nth-child(4) { width: 8%; }
.archive-table col:nth-child(5) { width: 9%; }
.failure-table col:nth-child(1) { width: 24%; }
.failure-table col:nth-child(2) { width: 16%; }
.failure-table col:nth-child(3) { width: 20%; }
.failure-table col:nth-child(4) { width: 20%; }
.failure-table col:nth-child(5) { width: 20%; }
@media print {
    body { background: #fff; }
    .page { margin: 0; max-width: none; }
    .card, .metric, .hero { box-shadow: none; }
}
@media (max-width: 1100px) {
    .summary-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
@media (max-width: 900px) {
    .summary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .mini-grid { grid-template-columns: 1fr; }
    .hero { align-items: flex-start; flex-direction: column; }
    .stamp { text-align: left; }
}
</style>
""";
    }

    private static void AddMetric(StringBuilder sb, string label, int value)
    {
        sb.AppendLine("<div class=\"metric\">");
        sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{value:N0}</div>");
        sb.AppendLine("</div>");
    }

    private static void AddMiniMetric(StringBuilder sb, string label, int value)
    {
        sb.AppendLine("<div class=\"mini-metric\">");
        sb.AppendLine($"<div class=\"label\">{Html(label)}</div>");
        sb.AppendLine($"<div class=\"value\">{value:N0}</div>");
        sb.AppendLine("</div>");
    }

    private static void AddKv(StringBuilder sb, string key, string value)
    {
        sb.AppendLine($"<tr><td>{Html(key)}</td><td>{Html(value)}</td></tr>");
    }

    private static void AddKvPath(StringBuilder sb, string key, string value)
    {
        sb.AppendLine($"<tr><td>{Html(key)}</td><td class=\"path\">{PathHtml(value)}</td></tr>");
    }

    private static void AddDecisionRow(StringBuilder sb, string decision, int count, string meaning)
    {
        sb.AppendLine($"<tr><td>{Html(decision)}</td><td class=\"num\">{count:N0}</td><td>{Html(meaning)}</td></tr>");
    }

    private static void AddConflictSection(StringBuilder sb, AuditReportRequest request)
    {
        List<ConsolidationGroup> conflicts = request.Groups
            .Where(g => g.Status == ConsolidationStatus.ConflictDifferentContent || g.Status == ConsolidationStatus.Error)
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (conflicts.Count == 0)
            return;

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Conflicts / Errors Requiring Review</h2>");
        sb.AppendLine("<table class=\"data-table\">");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Status</th><th>Reason</th><th class=\"num\">Candidates</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (ConsolidationGroup group in conflicts)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(group.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"bad status-cell\">{Html(FormatStatus(group.Status))}</td>");
            sb.AppendLine($"<td>{Html(group.DecisionReason)}</td>");
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

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Copy Failures</h2>");
        sb.AppendLine("<table class=\"data-table failure-table\">");
        sb.AppendLine("<colgroup><col><col><col><col><col></colgroup>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Reason</th><th>Source</th><th>Target</th><th>Status</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (AuditCopyRecord record in failed)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(record.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"bad\">{Html(record.Message)}</td>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(request.IncludeFullSourcePaths ? record.SourcePath : Path.GetFileName(record.SourcePath))}</td>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(record.DestinationPath)}</td>");
            sb.AppendLine("<td class=\"bad\">Failed</td>");
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

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Verification Failures</h2>");
        sb.AppendLine("<table class=\"data-table failure-table\">");
        sb.AppendLine("<colgroup><col><col><col><col><col></colgroup>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Failure</th><th>Message</th><th>Source</th><th>Target</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (CopyVerificationResult record in failed)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(record.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"bad\">{Html(record.Status.ToString())}</td>");
            sb.AppendLine($"<td>{Html(record.Message)}</td>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(request.IncludeFullSourcePaths ? record.SourcePath : Path.GetFileName(record.SourcePath))}</td>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(record.DestinationPath)}</td>");
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
                         g.Status == ConsolidationStatus.DuplicateSameContent))
            .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        sb.AppendLine("<section class=\"card\">");
        sb.AppendLine("<h2>Archive Decisions</h2>");
        sb.AppendLine("<table class=\"data-table archive-table\">");
        sb.AppendLine("<colgroup><col><col><col><col><col></colgroup>");
        sb.AppendLine("<thead><tr><th>Relative Path</th><th>Decision</th><th>Selected Source</th><th class=\"num\">Candidates</th><th class=\"num\">Size</th></tr></thead>");
        sb.AppendLine("<tbody>");

        foreach (ConsolidationGroup group in selectedGroups)
        {
            SourceFileRecord selected = group.SelectedFile!;
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(group.RelativePath)}</td>");
            sb.AppendLine($"<td class=\"status-cell\">{Html(FormatStatus(group.Status))}</td>");
            sb.AppendLine($"<td class=\"path\">{PathHtml(request.IncludeFullSourcePaths ? selected.FullPath : selected.SourceRoot)}</td>");
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

    private static string PathHtml(string? value)
    {
        string encoded = Html(value);
        return encoded
            .Replace("\\", "\\<wbr>")
            .Replace("/", "/<wbr>")
            .Replace("_", "_<wbr>")
            .Replace("-", "-<wbr>");
    }
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

    public string SourcePath { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public bool Success { get; set; }

    public bool Skipped { get; set; }

    public string Message { get; set; } = string.Empty;

    public long BytesCopied { get; set; }
}

public sealed class AuditReportResult
{
    public string HtmlReportPath { get; set; } = string.Empty;

    public string ReportFolder { get; set; } = string.Empty;
}
