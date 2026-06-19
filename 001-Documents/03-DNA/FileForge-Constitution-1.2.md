# The Constitution of FileForge

**Document Type:** Living Project Constitution  
**Project:** FileForge  
**Version:** 1.2  
**Status:** Active / Living Document  
**Current Development Branch:** `ux-refresh-v1`  
**Stable Branch:** `main`  

---

## 1. Purpose of this Constitution

This Constitution is the single governing Markdown document for FileForge.

It records the product philosophy, architecture decisions, safety rules, workflow rules, implementation discipline, roadmap, and frozen decisions made during development.

All major FileForge decisions must be updated here so that the project does not depend on scattered notes, forgotten chat context, or disconnected documents.

---

## 2. Product Identity

**FileForge** is a Windows desktop application for backup consolidation, deduplication, verification, and clean archive creation.

The name FileForge reflects the central idea:

> Forge a clean, reliable, verified archive from messy old file collections.

FileForge is not merely a copy utility. It is intended to become a trustworthy archive consolidation system.

---

## 3. Core Objective

The primary objective of FileForge is to help a user consolidate multiple old backup folders into one clean, verified archive while preserving the relative folder structure and avoiding unnecessary duplicate files.

FileForge must clearly answer:

1. What files exist across selected source folders?
2. Which files are unique?
3. Which files are true duplicates?
4. Which files have the same relative path but different content?
5. Which files will be copied to the archive?
6. Which files will be skipped?
7. Was the archive copied correctly?
8. What was verified, failed, blocked, skipped, or reported?

---

## 4. Technology Stack

FileForge currently uses:

- .NET 9
- C#
- Windows Forms
- Clean Architecture style project separation
- SHA256 hashing for content identity
- HTML report generation for end-user audit output

---

## 5. Solution Structure

```text
FileForge
│
├── FileForge.WinForms
│   ├── frmMain.cs
│   ├── frmMain.Designer.cs
│   ├── Program.cs
│   └── FileForge.WinForms.csproj
│
├── FileForge.Application
│   ├── Services
│   │   ├── FolderScanService.cs
│   │   ├── FileHashService.cs
│   │   ├── FileSelectionService.cs
│   │   ├── CopyVerificationService.cs
│   │   ├── TargetPreflightService.cs
│   │   └── ReportService.cs
│   └── FileForge.Application.csproj
│
├── FileForge.Domain
│   ├── Models
│   │   ├── SourceFileRecord.cs
│   │   ├── ConsolidationGroup.cs
│   │   ├── ConsolidationDecision.cs
│   │   └── VerificationResult.cs
│   └── FileForge.Domain.csproj
│
└── FileForge.Infrastructure
    ├── Hashing
    │   └── Sha256FileHasher.cs
    │
    ├── FileSystem
    │   └── FileCopyService.cs
    │
    └── FileForge.Infrastructure.csproj
```

---

## 6. Project Reference Rules

```text
FileForge.WinForms
    → FileForge.Application

FileForge.Application
    → FileForge.Domain
    → FileForge.Infrastructure

FileForge.Infrastructure
    → FileForge.Domain
```

The UI project shall not become the business logic engine.

---

## 7. Development Discipline

### 7.1 Full Replacement File Rule

When modifying source code, complete replacement files are preferred.

Avoid partial instructions such as:

```text
Insert this after line 120
Replace only this method
Add this block near the end
```

This rule prevents:

- missing braces;
- accidental duplicate methods;
- incomplete event wiring;
- merge confusion;
- loss of version control clarity.

### 7.2 Designer File Rule

Do not manually edit:

```text
frmMain.Designer.cs
```

All current WinForms controls are created directly in:

```text
frmMain.cs
```

### 7.3 Step-by-Step Rule

Major UI and functionality changes must be introduced one step at a time.

The successful FileForge recovery followed this order:

1. Header only.
2. Header + information cards.
3. Source and target panels.
4. Results grid.
5. Details panel.
6. Command row.
7. Status strip.
8. Source/target wiring.
9. Scan.
10. Analyze.
11. Copy.
12. Verify.
13. Target safety.
14. HTML audit report.
15. Options workflow.

This discipline must continue.

### 7.4 No Blind UI Rewrite Rule

Once a UI baseline is approved, future changes must not casually move controls, resize panels, or redesign layout unless the task is explicitly a UI/UX task.

---

## 8. Git Discipline

Current Git model:

```text
main            = stable functional baseline
ux-refresh-v1   = UI/UX improvement and current development branch
```

Important checkpoint:

```text
v0.1-ui-locked
```

Meaning:

```text
FileForge UI baseline locked with source/target panels, results grid, details panel, status strip, and multi-source picker.
```

### Recommended Git Flow

Before risky changes:

```bat
git status
git checkout -b feature-or-ux-branch-name
```

After a successful controlled change:

```bat
git add <changed-files>
git commit -m "Clear commit message"
git push
```

Do not continue several major changes without committing a known-good checkpoint.

---

## 9. Current Functional Capabilities

As of Version 1.2 of this Constitution, the following workflows are implemented and working:

```text
Source Root Folder Selection
Target Folder Selection
Recursive File Discovery
SHA256 Hashing
Relative Path Grouping
Duplicate Detection
Conflict Detection
Winner Selection
Copy to Master Archive
Copy Verification
Decision Details Panel
Target Preflight Safety Rule
Options Workflow
HTML Audit Report Generation
HTML Report Opening in Browser
```

---

## 10. Source Selection Rule

The user selects source root folders, not individual files.

All files and subfolders under each selected source root are included automatically during scan.

The relative folder structure under each source root must be preserved in the target archive.

---

## 11. Multi-Source Folder Selection

The application supports selecting multiple source folders using the native Windows multi-folder picker.

Expected interactions:

```text
CTRL + Click     = select multiple folders
SHIFT + Click    = select a range
```

The selected source list also supports multi-select removal.

---

## 12. Relative Path Rule

FileForge compares files based on their path relative to the selected source root.

Example:

```text
SourceA\Documents\Invoice.pdf
SourceB\Documents\Invoice.pdf
```

Both files have the same relative path:

```text
Documents\Invoice.pdf
```

Therefore, they belong to the same logical consolidation group.

---

## 13. Hashing Rule

Filename and relative path alone are not enough to determine duplication.

Two files may have the same relative path but different content.

Therefore FileForge uses SHA256 hashing to determine whether two same-relative-path files are truly identical.

### Hashing Strategy

Hashing must be selective and performance-aware:

1. Scan files first.
2. Group by relative path.
3. If a group has only one file, hashing is not required for duplicate comparison.
4. If a group has multiple files, compare size first.
5. If sizes differ, classify as conflict without hashing.
6. If sizes match, compute hash to confirm whether content is identical.

Hashing must run asynchronously so the UI does not freeze.

---

## 14. Decision Classification

Current core classifications:

```text
Unique
Duplicate Same Content
Conflict
```

Future classifications may include:

```text
Already Archived
Target Conflict
Archive Only Existing File
Copy Error
Verification Failed
```

---

## 15. Winner Selection Rule

For duplicate files with the same relative path and same content hash, only one copy shall be archived.

Current deterministic winner rule:

1. Prefer the earliest selected source root when duplicate content is identical.
2. Skip equivalent duplicates from later source roots.
3. Record duplicate sources in decision details and audit report.

For non-identical conflicts, FileForge must not guess silently. It must report the conflict clearly.

---

## 16. Copy Rule

The Copy workflow copies only files approved for archive creation.

Current V1 copy candidates:

```text
Unique files
Duplicate Same Content winner files
```

Copy shall skip:

```text
Duplicate non-winner files
Conflict files
Invalid files
Blocked target files
```

Relative folder structure must be preserved.

---

## 17. Empty Directory Rule

FileForge supports the concept of preserving empty directories, but V1 default behaviour is file-centric.

### Backup Archive Mode

```text
Preserve Empty Directories = OFF
```

Behaviour:

- Copy only files.
- Create folders only when needed for copied files.
- Ignore empty source folders.

### Full Folder Reconstruction Mode

```text
Preserve Empty Directories = ON
```

Behaviour:

- Copy files.
- Recreate empty source folders.
- Preserve source folder hierarchy more completely.

Default:

```text
Unchecked / OFF
```

---

## 18. Target Safety Rule V1

FileForge V1 shall operate in **New Archive Mode**.

The target folder is treated as a clean archive destination, not as a merge/update folder and not as an existing archive baseline.

### Mandatory Target Safety Rules

Before Copy is allowed:

1. The target folder must not be the same as any selected source root.
2. The target folder must not be inside any selected source root.
3. No selected source root must be inside the target folder.
4. The target folder must be empty before Copy is allowed.
5. FileForge shall never overwrite existing target files automatically.

### Rationale

These rules prevent:

- recursive archive contamination;
- accidental self-copying;
- duplicate amplification;
- overwrite of existing files;
- ambiguity between source data and archive output;
- false verification results caused by pre-existing target files.

### Invalid Example — Target Inside Source

```text
Source: E:\OldBackups
Target: E:\OldBackups\FileForgeArchive
```

This is invalid because the archive destination is inside the source tree.

### Invalid Example — Source Inside Target

```text
Source: E:\Archive\OldLaptop
Target: E:\Archive
```

This is invalid because the target already contains one of the selected sources.

### Invalid Example — Target Not Empty

```text
Target: E:\FileForgeArchive
Existing content:
- OldReport.pdf
- Photos\Image001.jpg
```

This is invalid in V1 because FileForge creates a clean new archive only.

### Required Application Behaviour

If the target violates any safety rule, FileForge shall block Copy and show a clear warning.

Recommended warning:

```text
Invalid target folder.

The target archive folder cannot be the same as, inside, or parent of any selected source folder.

Please choose a separate empty folder outside the selected source roots.
```

If the target is not empty:

```text
Target folder is not empty.

FileForge V1 creates a clean new archive only.
Please choose an empty folder or create a new archive folder.
```

---

## 19. Future Target Mode — V2 Existing Archive Baseline

A future version may support **Update Existing Archive Mode**.

In that mode, the target folder may contain files and may be treated as an existing archive baseline.

Potential classifications:

```text
Already Archived
New File
Target Conflict
Archive Only Existing File
```

This is deferred and must not be mixed into V1.

V1 remains:

```text
New Archive Mode only.
Target must be empty.
```

---

## 20. Verification Rule

Verify answers one question:

> Did the files selected for archive creation arrive correctly in the target archive?

Verification checks:

1. Target file exists.
2. Target size matches source.
3. Target SHA256 hash matches source.

Verification must not:

- rescan source folders;
- re-run duplicate analysis;
- change winner selection;
- silently correct copy problems.

Verification must report failures clearly with:

```text
Relative Path
Source Path
Target Path
Failure Reason
Expected Size
Actual Size
Source Hash
Target Hash
```

---

## 21. Information Bar Semantics

The information bar should explain archive meaning, not merely show raw numbers.

Current intended cards:

```text
Sources
Total Files
To Archive
Dup. Skipped
Conflicts
Verified
```

Meaning:

- **Sources**: number of selected source root folders.
- **Total Files**: physical files scanned.
- **To Archive**: files expected to be copied/verified.
- **Dup. Skipped**: duplicate physical files skipped.
- **Conflicts**: same-relative-path conflicts requiring user attention.
- **Verified**: files successfully verified in target.

---

## 22. UI/UX Principle

FileForge must guide the user through a clear workflow:

```text
1 Scan → 2 Analyze → 3 Copy → 4 Verify → 5 Report
```

The UI should not feel like a random collection of buttons.

The user should always know:

```text
What step am I on?
What happened?
What is safe to do next?
What requires attention?
```

The current UI is functional but not yet product-grade. A serious UX refresh is deferred to a separate dedicated session.

---

## 23. Current UI Locked Structure

The approved current UI structure contains:

```text
Header
Statistics ribbon
Command row
Source Root Folders panel
Target Master Folder panel
Preview / Results grid
Details panel
Status strip
```

No Designer file edits are allowed.

---

## 24. Options Workflow V1

The Options button opens a simple options dialog.

Current V1 options include:

```text
Preserve Empty Directories
Open target folder after successful Copy
Open HTML audit report after generation
Include full source paths in audit report
```

The target mode shall be shown as locked:

```text
Target Mode: New Archive Mode
Target must be empty before Copy
```

Existing Archive Baseline Mode is deferred to V2.

---

## 25. Report / Audit Engine V1

### 25.1 Report Format

FileForge V1 shall generate a professional **HTML audit report**.

Markdown is not required for end-user audit reports.

Rationale:

- FileForge is not development software.
- End users should not need VS Code, Markdown viewers, or developer tools.
- HTML can be opened directly in a browser.
- HTML can be printed or saved as PDF using the browser.
- HTML is easier to share with ordinary users.

### 25.2 Output Location

The report shall be generated under the selected target folder:

```text
TargetFolder\FileForge_Report\FileForge_Audit_Report_yyyyMMdd_HHmmss.html
```

### 25.3 Report Opening Rule

After report generation, FileForge V1 should open the HTML report in the user’s default browser when the relevant option is enabled.

In-app report preview is not part of V1.

### 25.4 Browser Opening Note

If Windows has `.html` files associated with VS Code or another editor, FileForge should attempt to open the report using a browser-friendly file URI approach.

If that still opens the wrong application, future improvement may allow explicit browser selection.

### 25.5 Report Content

The HTML audit report should include:

- application name;
- generated date/time;
- application mode;
- target safety rule summary;
- selected source roots;
- target folder;
- scan summary;
- analysis summary;
- copy summary;
- verification summary;
- archive decisions;
- conflicts;
- verification failures;
- skipped duplicates;
- hash algorithm;
- report generation timestamp.

### 25.6 Report Layout Rule

The HTML report must handle long file paths professionally.

Report tables must not allow long paths to visually overflow outside the page.

Required behaviour:

```text
Long paths wrap or break safely.
Tables remain inside page width.
Report remains readable in browser and printable to PDF.
```

### 25.7 Future V2 Report Viewer

A future V2 feature may add:

```text
In-app report preview / modal report viewer
```

This is deferred because it may require:

- WebView2 or similar dependency;
- print handling;
- zoom handling;
- modal resizing;
- extra UI maintenance.

V1 shall use the external browser.

---

## 26. Report / Audit Engine — Future Enhancements

Future report enhancements may include:

```text
PDF export
CSV export
Excel export
Report templates
Report signing / checksum
Printable executive summary
Detailed technical appendix
```

---

## 27. Roadmap

### V1 Foundation

```text
UI locked
Scan
Analyze
Copy
Verify
Target Safety Rule
Basic statistics
Decision details
HTML audit report
Options workflow
```

### V1 Next

```text
Better conflict drilldown
Refined report styling
Formal test checklist
Controlled UX refresh planning
```

### V2 Candidate Features

```text
Existing Archive Baseline Mode
Target merge/update workflow
Conflict versioning
Delete/Reclaim Storage workflow
Dashboard
Advanced audit trail
Profile presets
In-app report preview/modal
PDF export
Excel/CSV export
```

---

## 28. Non-Negotiable Product Principles

FileForge must be:

1. Safe before clever.
2. Deterministic before flexible.
3. Explainable before automated.
4. Verification-first.
5. Non-destructive by default.
6. User-guided, not guess-driven.
7. Able to produce an audit trail.
8. Designed for ordinary users but safe enough for serious archive work.

---

## 29. Session Continuity Notes

At the end of each major session, update this Constitution with:

- new frozen rules;
- new implementation decisions;
- completed Git checkpoints;
- current branch;
- next priority;
- known defects;
- deferred items.

This document is the living memory of FileForge.

---

## 30. Current Session Update — Report and Options

The following decisions are now frozen:

1. FileForge V1 end-user audit report shall be HTML only.
2. Markdown is not required for user audit reports.
3. The HTML report shall open in the default browser.
4. In-app report preview/modal is deferred to V2.
5. Options workflow is introduced for practical user preferences.
6. Long file paths in the HTML report must wrap safely and must not overflow outside the page.
7. UX refresh remains deferred to a separate dedicated session because current functionality is more important than visual polish at this stage.

