# FileForge Vision Document

**Document Type:** Product Vision  
**Project:** FileForge  
**Status:** Draft v0.1  
**Target Location:** `001-Documents\01-Foundation\FileForge-Vision.md`

---

## 1. Vision Statement

FileForge exists to become the trusted consolidation engine for messy, scattered, duplicated, and uncertain file collections.

Our vision is the sky in consolidation.

FileForge shall not remain a simple copy utility. It shall grow into a professional system that helps users understand, consolidate, verify, preserve, and trust their digital archives.

The long-term vision is simple:

> Take disorderly file collections and forge them into a clean, verified, explainable, and trustworthy archive.

---

## 2. Core Belief

People accumulate files over years across laptops, phones, backup drives, cloud downloads, old project folders, repeated exports, duplicate photos, copied documents, renamed files, and half-organized archives.

Most users do not know:

- which files are unique;
- which files are duplicates;
- which files are older copies;
- which files have changed;
- which files are safe to skip;
- which files must be preserved;
- whether an archive copy can be trusted.

FileForge exists to solve this problem.

The application must give users confidence, not confusion.

---

## 3. Product Purpose

The purpose of FileForge is to provide safe, explainable, verified archive consolidation and file truth discovery, including duplication.

FileForge must answer:

1. What files exist?
2. Where do they exist?
3. Which files are identical?
4. Which files are duplicated?
5. Which files are same-path conflicts?
6. Which files may be related or renamed versions?
7. Which files should be archived?
8. Which files were skipped and why?
9. Which files were preserved in a conflict vault?
10. Which files were copied successfully?
11. Which files were verified?
12. What can the user trust?

---

## 4. Product Positioning

FileForge is not a file copier.

FileForge is not a basic duplicate cleaner.

FileForge is not a blind automation tool.

FileForge is a file consolidation and archive trust system.

Its role is to combine:

- archive consolidation;
- duplicate discovery;
- conflict preservation;
- checksum verification;
- audit reporting;
- file truth explanation;
- future intelligent file relationship detection.

---

## 5. Long-Term Ambition

The ambition of FileForge is to become a complete consolidation platform.

The product may grow through several layers.

### Layer 1 — Archive Consolidation

Consolidate multiple backup folders into one clean archive while preserving folder structure and avoiding unnecessary duplicate copies.

### Layer 2 — Verification and Audit

Verify copied files using deterministic checks and produce an audit report that explains what happened.

### Layer 3 — Duplicate Intelligence

Identify duplicate files inside one folder or across many folders, including files with the same content but different locations.

### Layer 4 — Conflict Intelligence

Detect same-relative-path files with different content, preserve the latest version in the main archive, and safely store older versions in a conflict vault.

### Layer 5 — Existing Archive Baseline

In a future version, compare new file collections against an existing archive and classify files as already archived, new, changed, missing, or conflicting.

### Layer 6 — AI-Assisted File Relationship Discovery

In a later version, AI may assist in identifying related files, renamed files, similar documents, possible version families, and near-duplicate content.

AI shall assist human review. It shall not become the truth engine.

---

## 6. Feature Discipline Rule

Features shall be added slowly and deliberately.

A feature is acceptable only if it strengthens FileForge’s core objective:

> safe, explainable, verified archive consolidation and file truth discovery, including duplication.

FileForge shall not become a random utility toolbox.

Every feature must support at least one of the following:

- archive safety;
- file truth;
- duplication detection;
- conflict handling;
- verification;
- audit reporting;
- user trust;
- controlled consolidation.

---

## 7. Truth Engine Principle

FileForge must remain deterministic.

The truth engine must be based on reliable technical evidence such as:

- file size;
- relative path;
- last modified date;
- SHA256 hash;
- copy verification;
- structured decision records;
- audit trail.

CRC or other checksums may be added as supporting information, but SHA256 shall remain the authoritative verification and content identity method unless a future technical decision deliberately changes this rule.

AI may suggest possible relationships, but AI must not silently decide archive truth, deletion truth, or verification truth.

---

## 8. Workflow Vision

FileForge should support more than one workflow, but each workflow must remain clear.

### Archive Consolidation Mode

Used when the user wants to create a clean archive from multiple source folders.

Workflow:

```text
Scan → Analyze → Copy → Verify → Report
```

Purpose:

```text
Multiple messy folders → one clean verified archive
```

### Duplicate Audit Mode

Used when the user wants to inspect duplication inside one folder or across selected folders.

Workflow:

```text
Scan → Analyze Duplicates → Review → Report
```

Purpose:

```text
One or more folders → duplicate truth and space-waste visibility
```

Initial Duplicate Audit Mode shall be report-only.

It shall not delete files.

It shall not automatically remove different-name duplicates.

It shall not automatically skip files from archive output.

### Future Existing Archive Mode

Used when the user wants to compare new source folders against an existing archive.

Purpose:

```text
New files + existing archive → update decision, conflict decision, verification decision
```

This is a future mode and shall not be mixed into V1 New Archive Mode.

---

## 9. Single Folder and Multiple Folder Direction

FileForge may route workflow intelligently based on selected source count.

### Single Source Folder

If one source folder is selected, FileForge may offer or default to Duplicate Audit Mode.

Purpose:

```text
One folder tree → find internal duplication and file truth
```

Initial behavior should be audit-only.

### Multiple Source Folders

If two or more source folders are selected, FileForge should default to Archive Consolidation Mode.

Purpose:

```text
Multiple backup roots → consolidate into one verified archive
```

A future version may allow the user to override the suggested mode.

---

## 10. Safety Philosophy

FileForge must be safe before clever.

The product must never create hidden data loss.

FileForge must not:

- silently overwrite files;
- silently delete files;
- silently skip conflicts;
- pretend verification succeeded when it did not;
- hide conflict vault copies;
- confuse archive decisions with verification status;
- allow AI to make irreversible decisions;
- produce reports that lie.

Every major action must be explainable.

Every copied file must be verifiable.

Every skipped file must have a reason.

Every conflict must be preserved or clearly reported.

---

## 11. User Trust Promise

FileForge must earn user trust by being honest.

The user should always know:

- what was scanned;
- what was analyzed;
- what was copied;
- what was skipped;
- what was preserved;
- what was verified;
- what failed;
- what needs review.

The application should not overwhelm the user, but it must never hide the truth.

The final archive must not be based on guesswork.

---

## 12. Future AI Direction

AI can become valuable in FileForge, but only after the deterministic foundation is strong.

Potential AI-assisted features:

- detecting similar file names;
- identifying likely renamed files;
- grouping possible document versions;
- detecting near-duplicate documents;
- suggesting file families;
- summarizing archive health;
- explaining duplicate patterns in plain language;
- helping the user review questionable file groups.

AI output must be labelled as advisory.

Approved language:

```text
Possible Match
Likely Related
Needs Review
Suggested Group
```

Forbidden language for AI-only decisions:

```text
Verified Duplicate
Safe to Delete
Definitely Same
Automatically Skip
```

AI may guide.

AI must not silently decide.

---

## 13. Strategic Direction

FileForge should grow from a working archive consolidation tool into a trusted file intelligence system.

The roadmap should move in controlled steps:

1. Stable archive consolidation.
2. Runtime hardening.
3. Large file-set testing.
4. Better audit reporting.
5. Duplicate audit mode.
6. Existing archive comparison.
7. Enhanced checksum outputs.
8. Professional release readiness.
9. AI-assisted file relationship discovery.
10. Enterprise-grade audit and compliance options.

Each step must be tested, explainable, and reversible.

---

## 14. Non-Negotiable Vision Principles

FileForge shall remain:

1. Safe.
2. Deterministic.
3. Explainable.
4. Auditable.
5. Verification-first.
6. Non-destructive by default.
7. User-guided.
8. Honest in UI.
9. Honest in reports.
10. Focused on consolidation and file truth.

The sky is the ambition.

Safety is the foundation.

Trust is the product.
