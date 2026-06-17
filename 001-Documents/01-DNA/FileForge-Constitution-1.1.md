# The Constitution of FileForge

**Document Type:** Living Project Constitution  
**Project:** FileForge  
**Current Version:** v0.1  
**Status:** Active / Living Document  
**Primary Branch Context:** `main` = stable baseline, `ux-refresh-v1` = UI/UX improvement branch  

---

## 1. Purpose of this Constitution

This document is the single governing Markdown document for FileForge.

It shall be updated whenever the project makes an important decision about:

- product purpose;
- architecture;
- safety rules;
- user experience;
- workflow;
- file handling;
- verification;
- reporting;
- development discipline;
- release planning;
- future roadmap.

The objective is to avoid scattered notes, forgotten decisions, and repeated re-discussion of already settled matters.

---

## 2. Product Identity

**FileForge** is a Windows desktop application for backup consolidation, deduplication, verification, and clean archive creation.

The name FileForge reflects the core purpose:

> To forge a clean, reliable, verified archive from messy old file collections.

FileForge is not merely a copy tool. It is intended to become a trustworthy archive consolidation system.

---

## 3. Core Objective

The primary objective of FileForge is to help a user consolidate multiple old backup folders into one clean archive while preserving the relative folder structure and avoiding unnecessary duplicate files.

The application must answer these questions clearly:

1. What files exist across the selected source folders?
2. Which files are unique?
3. Which files are true duplicates?
4. Which files have the same relative path but different content?
5. Which file should be copied into the archive?
6. Was the archive copied correctly?
7. What was skipped, copied, verified, or blocked?

---

## 4. Technology Stack

FileForge is currently built using:

- .NET 9
- Windows Forms
- C#
- Clean Architecture style project separation

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

The UI project must not become the business logic engine.

---

## 7. Development Discipline

### 7.1 Full Replacement File Rule

When modifying source code, the preferred delivery format is a complete replacement file.

Avoid partial snippets such as:

```text
Insert this after line 120
Replace only this method
Add this block near the end
```

This rule exists to avoid:

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

### 7.3 One Step at a Time Rule

Major UI and functionality changes must be done step by step.

The successful FileForge UI recovery followed this order:

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

This discipline must be preserved.

### 7.4 No Blind UI Rewrite Rule

Once a UI baseline is approved, future changes must not casually move controls, resize panels, or redesign the layout unless the task is explicitly a UI/UX branch task.

---

## 8. Git Discipline

Current established Git model:

```text
main            = stable functional baseline
ux-refresh-v1   = UI/UX improvement branch
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

After successful controlled change:

```bat
git add <changed-files>
git commit -m "Clear commit message"
git push
```

Never continue several major changes without committing a known-good checkpoint.

---

## 9. Current Functional Capabilities

As of this constitution version, the following workflows are implemented and working:

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
```

---

## 10. Source Selection Rule

The user selects source root folders, not individual files.

All files and subfolders under each selected source root are included automatically during scan.

The relative folder structure under each source root must be preserved in the target archive.

---

## 11. Multi-Source Folder Selection

The application supports selecting multiple source folders using the native Windows multi-folder picker.

Expected user interactions:

```text
CTRL + Click     = select multiple folders
SHIFT + Click    = select range
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

Therefore they belong to the same logical consolidation group.

---

## 13. Hashing Rule

Filename and relative path alone are not enough to determine duplication.

Two files may have the same relative path but different content.

Therefore FileForge uses SHA256 hashing to determine whether two same-relative-path files are truly identical.

### Hashing Strategy

Hashing should be selective and performance-aware:

1. Scan files first.
2. Group by relative path.
3. If a group has only one file, hashing is not required for duplicate comparison.
4. If a group has multiple files, compare size first.
5. If sizes differ, classify as conflict without hashing.
6. If sizes match, compute hash to confirm whether content is identical.

Hashing should run asynchronously so the UI does not freeze.

---

## 14. Decision Classification

FileForge shall classify scanned files into clear decision categories.

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
3. Record duplicate sources in decision details.

For future non-identical conflicts, FileForge must not guess silently. It must report the conflict clearly.

---

## 16. Copy Rule

The Copy workflow copies only files that are approved for archive creation.

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

The information bar should not merely show raw numbers. It should explain archive meaning.

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

---

## 23. Current UI Locked Structure

The approved UI structure contains:

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

## 24. Report / Audit Engine — Future Priority

The next major functional area is the Report / Audit Engine.

The report should eventually include:

- selected source roots;
- target folder;
- scan summary;
- duplicate summary;
- conflict summary;
- copy summary;
- verification summary;
- target safety preflight result;
- skipped files;
- failed files;
- timestamp;
- application version;
- hash algorithm used.

---

## 25. Roadmap

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
```

### V1 Next

```text
Report / Audit Engine
Better conflict drilldown
Improved UX refresh
Export report to Markdown / CSV / PDF later
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
```

---

## 26. Non-Negotiable Product Principles

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

## 27. Session Continuity Notes

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

## 28. Report / Audit Engine Decision — V1 and V2

### V1 Report Format

FileForge V1 shall generate a **professional HTML audit report** for end users.

Markdown reports are not required for the user-facing Report/Audit Engine because FileForge is not a development tool. Markdown remains useful only for internal project documentation such as this Constitution.

### V1 Report Behaviour

When the user clicks **Report**, FileForge shall:

1. Create a `FileForge_Report` folder inside the selected target archive folder.
2. Generate an HTML audit report named using the current timestamp.
3. Save the report as:

```text
TargetFolder\FileForge_Report\FileForge_Audit_Report_yyyyMMdd_HHmmss.html
```

4. Open the generated HTML report in the user's default browser, if the relevant option is enabled.

### V1 Rationale

Opening the report in the default browser gives the user:

- immediate viewing;
- browser zoom;
- printing;
- save-as-PDF through the browser;
- easy sharing of the generated HTML file.

### V2 Deferred Feature

An in-application report preview window or modal is deferred to V2.

Reason for deferral:

- it may require WebView2 or another embedded viewer;
- it adds another UI surface to maintain;
- print/zoom/export handling becomes more complex;
- FileForge V1 should prioritize safety, correctness, verification, and audit generation over embedded preview polish.

V2 candidate:

```text
Report Preview inside FileForge
```

V1 rule remains:

```text
Generate HTML audit report and open it in the default browser.
```

