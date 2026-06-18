using FileForge.Application.Services;
using FileForge.Domain.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FileForge.WinForms
{
    public class frmMainApp : Form
    {
        private readonly Color _pageBack = Color.FromArgb(245, 248, 252);
        private readonly Color _cardBack = Color.White;
        private readonly Color _border = Color.FromArgb(204, 214, 228);
        private readonly Color _text = Color.FromArgb(20, 35, 58);
        private readonly Color _muted = Color.FromArgb(88, 103, 128);
        private readonly Color _blue = Color.FromArgb(30, 96, 190);
        private readonly Color _green = Color.FromArgb(18, 134, 76);

        private Panel pnlHeader = null!;
        private Panel pnlToolbar = null!;
        private Panel pnlSource = null!;
        private Panel pnlTarget = null!;
        private Panel pnlSummary = null!;
        private Panel pnlResults = null!;
        private Panel pnlDetails = null!;
        private Panel pnlStatus = null!;

        private Label lblTitle = null!;
        private Label lblTagline = null!;
        private Button btnOptions = null!;

        private Button btnScan = null!;
        private Button btnAnalyze = null!;
        private Button btnCopy = null!;
        private Button btnVerify = null!;
        private Button btnReport = null!;

        private Label lblSourceTitle = null!;
        private Label lblSourceSub = null!;
        private Button btnAddSource = null!;
        private Button btnRemoveSource = null!;
        private Button btnClearSources = null!;
        private ListBox lstSources = null!;

        private Label lblTargetTitle = null!;
        private Label lblTargetSub = null!;
        private TextBox txtTarget = null!;
        private Button btnChangeTarget = null!;
        private Button btnBrowseTarget = null!;
        private Button btnOpenTarget = null!;
        private Label lblSafety = null!;

        private DataGridView gridResults = null!;
        private TextBox txtDetails = null!;

        private Label lblStatusTitle = null!;
        private Label lblStatusMessage = null!;

        private MetricCard metricTotalFiles = null!;
        private MetricCard metricToArchive = null!;
        private MetricCard metricDuplicates = null!;
        private MetricCard metricConflicts = null!;
        private MetricCard metricVerified = null!;

        private ComboBox cmbDecisionFilter = null!;
        private TextBox txtSearch = null!;

        private readonly FolderScanService _folderScanService = new();
        private readonly FileHashService _fileHashService = new();
        private readonly CopyVerificationService _copyVerificationService = new();
        private readonly TargetPreflightService _targetPreflightService = new();
        private readonly ReportService _reportService = new();

        private readonly List<SourceFileRecord> _scannedFiles = new();
        private readonly List<ConsolidationGroup> _groups = new();
        private readonly Dictionary<string, AppCopyResult> _copyResultsByRelativePath = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AppCopyResult> _copyResultRecords = new();
        private readonly Dictionary<string, CopyVerificationResult> _verificationResultsByRelativePath = new(StringComparer.OrdinalIgnoreCase);

        private AppResultsMode _resultsMode = AppResultsMode.Scan;
        private bool _isBusy;
        private bool _preserveEmptyDirectories;
        private bool _openTargetFolderAfterCopy;
        private bool _openAuditReportAfterGeneration = true;
        private bool _includeFullSourcePathsInReport = true;

        public frmMainApp()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "FileForge - Professional File Consolidation";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1500, 900);
            MinimumSize = new Size(1200, 760);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            BackColor = _pageBack;

            Controls.Clear();

            BuildHeader();
            BuildToolbar();
            BuildSourceCard();
            BuildTargetCard();
            BuildSummary();
            BuildResults();
            BuildDetails();
            BuildStatus();

            Resize += (_, _) => LayoutUi();
            Shown += (_, _) => LayoutUi();

            LayoutUi();
            WireUiEvents();
            UpdateUiState();
            SetStatus("Ready", "Select source folders and an archive target.");
        }

        private void BuildHeader()
        {
            pnlHeader = new Panel
            {
                BackColor = _pageBack
            };
            Controls.Add(pnlHeader);

            lblTitle = new Label
            {
                Text = "FileForge",
                AutoSize = true,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 47, 93)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblTagline = new Label
            {
                Text = "Consolidate  •  Deduplicate  •  Verify  •  Archive",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = _muted
            };
            pnlHeader.Controls.Add(lblTagline);

            btnOptions = CreateHeaderButton("⚙  Options");
            pnlHeader.Controls.Add(btnOptions);
        }

        private void BuildToolbar()
        {
            pnlToolbar = new Panel
            {
                BackColor = _cardBack
            };
            pnlToolbar.Paint += PaintBottomBorder;
            Controls.Add(pnlToolbar);

            btnScan = CreateToolbarButton("Scan", ToolbarIcon.Scan);
            btnAnalyze = CreateToolbarButton("Analyze", ToolbarIcon.Analyze);
            btnCopy = CreateToolbarButton("Copy", ToolbarIcon.Copy);
            btnVerify = CreateToolbarButton("Verify", ToolbarIcon.Verify);
            btnReport = CreateToolbarButton("Report", ToolbarIcon.Report);

            pnlToolbar.Controls.AddRange(new Control[]
            {
                btnScan, btnAnalyze, btnCopy, btnVerify, btnReport
            });
        }

        private void BuildSourceCard()
        {
            pnlSource = CreateCard();
            Controls.Add(pnlSource);

            lblSourceTitle = CreateSectionTitle("Source Folders");
            pnlSource.Controls.Add(lblSourceTitle);

            lblSourceSub = CreateSmallText("No source folders selected.");
            pnlSource.Controls.Add(lblSourceSub);

            btnAddSource = CreateSecondaryButton("+  Add Source");
            pnlSource.Controls.Add(btnAddSource);

            lstSources = new ListBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                HorizontalScrollbar = true,
                SelectionMode = SelectionMode.MultiExtended,
                BackColor = Color.White
            };
            pnlSource.Controls.Add(lstSources);

            btnRemoveSource = CreateSecondaryButton("Remove Selected");
            pnlSource.Controls.Add(btnRemoveSource);

            btnClearSources = CreateSecondaryButton("Clear All");
            pnlSource.Controls.Add(btnClearSources);
        }

        private void BuildTargetCard()
        {
            pnlTarget = CreateCard();
            Controls.Add(pnlTarget);

            lblTargetTitle = CreateSectionTitle("Archive Target");
            pnlTarget.Controls.Add(lblTargetTitle);

            lblTargetSub = CreateSmallText("Target folder not selected.");
            pnlTarget.Controls.Add(lblTargetSub);

            btnChangeTarget = CreateSecondaryButton("Change Target");
            pnlTarget.Controls.Add(btnChangeTarget);

            btnOpenTarget = CreateSecondaryButton("Open Target");
            pnlTarget.Controls.Add(btnOpenTarget);

            var lblTarget = new Label
            {
                Text = "Target Folder:",
                AutoSize = true,
                ForeColor = _text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            pnlTarget.Controls.Add(lblTarget);
            lblTarget.Name = "lblTargetFolder";

            txtTarget = new TextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };
            pnlTarget.Controls.Add(txtTarget);

            btnBrowseTarget = CreateDarkButton("...");
            pnlTarget.Controls.Add(btnBrowseTarget);

            lblSafety = new Label
            {
                Text = "● Target Safety: Not checked yet",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                BackColor = _cardBack,
                ForeColor = _muted,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            pnlTarget.Controls.Add(lblSafety);

        }

        private void BuildSummary()
        {
            pnlSummary = CreateCard();
            Controls.Add(pnlSummary);

            metricTotalFiles = new MetricCard("Total Files Found", "0", Color.FromArgb(30, 96, 190), MetricIcon.File);
            metricToArchive = new MetricCard("To Archive", "0", Color.FromArgb(18, 134, 76), MetricIcon.Archive);
            metricDuplicates = new MetricCard("Duplicates Skipped", "0", Color.FromArgb(96, 55, 180), MetricIcon.Duplicate);
            metricConflicts = new MetricCard("Conflicts Auto-Resolved", "0", Color.FromArgb(230, 105, 15), MetricIcon.Conflict);
            metricVerified = new MetricCard("Verified", "0", Color.FromArgb(18, 134, 76), MetricIcon.Verify);

            pnlSummary.Controls.AddRange(new Control[]
            {
                metricTotalFiles, metricToArchive, metricDuplicates, metricConflicts, metricVerified
            });
        }

        private void BuildResults()
        {
            pnlResults = CreateCard();
            Controls.Add(pnlResults);

            var title = CreateSectionTitle("Results");
            title.Name = "lblResultsTitle";
            pnlResults.Controls.Add(title);

            var subtitle = CreateSmallText("No analysis completed yet.");
            subtitle.Name = "lblResultsSub";
            pnlResults.Controls.Add(subtitle);

            cmbDecisionFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                Name = "cmbDecisionFilter"
            };
            cmbDecisionFilter.Items.AddRange(new object[] { "All Decisions", "Unique", "Duplicates", "Conflicts", "Verified", "Failed" });
            cmbDecisionFilter.SelectedIndex = 0;
            pnlResults.Controls.Add(cmbDecisionFilter);

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 9F),
                Text = "",
                Name = "txtSearch"
            };
            pnlResults.Controls.Add(txtSearch);

            gridResults = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 28
            };

            gridResults.Columns.Add("colNo", "#");
            gridResults.Columns.Add("colRelativePath", "Relative Path");
            gridResults.Columns.Add("colDecision", "Decision");
            gridResults.Columns.Add("colMainSource", "Main Archive Source");
            gridResults.Columns.Add("colOther", "Other Sources / Info");
            gridResults.Columns.Add("colSize", "Size");
            gridResults.Columns.Add("colModified", "Modified");

            gridResults.Columns["colNo"].FillWeight = 8;
            gridResults.Columns["colRelativePath"].FillWeight = 34;
            gridResults.Columns["colDecision"].FillWeight = 24;
            gridResults.Columns["colMainSource"].FillWeight = 30;
            gridResults.Columns["colOther"].FillWeight = 32;
            gridResults.Columns["colSize"].FillWeight = 14;
            gridResults.Columns["colModified"].FillWeight = 18;

            pnlResults.Controls.Add(gridResults);
        }

        private void BuildDetails()
        {
            pnlDetails = CreateCard();
            Controls.Add(pnlDetails);

            var title = CreateSectionTitle("Details");
            title.Name = "lblDetailsTitle";
            pnlDetails.Controls.Add(title);

            var subtitle = CreateSmallText("Select a file above to see details and decision explanation.");
            subtitle.Name = "lblDetailsSub";
            pnlDetails.Controls.Add(subtitle);

            txtDetails = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F),
                WordWrap = false,
                Text = "Decision details will appear here."
            };
            pnlDetails.Controls.Add(txtDetails);
        }

        private void BuildStatus()
        {
            pnlStatus = new Panel
            {
                BackColor = _cardBack,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(pnlStatus);

            lblStatusTitle = new Label
            {
                Text = "Ready",
                AutoSize = true,
                ForeColor = _green,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            pnlStatus.Controls.Add(lblStatusTitle);

            lblStatusMessage = new Label
            {
                Text = "Select source folders and an archive target.",
                AutoSize = true,
                ForeColor = _text,
                Font = new Font("Segoe UI", 9F)
            };
            pnlStatus.Controls.Add(lblStatusMessage);
        }

        private void LayoutUi()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            int margin = 18;
            int gap = 12;
            int headerH = 74;
            int toolbarH = 46;
            int setupH = 158;
            int summaryH = 68;
            int statusH = 38;

            pnlHeader.SetBounds(0, 0, w, headerH);
            lblTitle.Location = new Point(margin, 8);
            lblTagline.Location = new Point(margin + 2, 43);
            btnOptions.SetBounds(w - margin - 116, 18, 116, 32);

            pnlToolbar.SetBounds(0, headerH, w, toolbarH);
            LayoutToolbarButtons(margin, 8);

            int setupY = headerH + toolbarH + gap;
            int contentW = w - (margin * 2);
            int cardW = (contentW - gap) / 2;

            pnlSource.SetBounds(margin, setupY, cardW, setupH);
            pnlTarget.SetBounds(margin + cardW + gap, setupY, cardW, setupH);

            LayoutSourceCard();
            LayoutTargetCard();

            int summaryY = setupY + setupH + gap;
            pnlSummary.SetBounds(margin, summaryY, contentW, summaryH);
            LayoutSummary();

            int statusY = h - margin - statusH;
            pnlStatus.SetBounds(margin, statusY, contentW, statusH);
            lblStatusTitle.Location = new Point(16, 8);
            lblStatusMessage.Location = new Point(128, 8);

            int workY = summaryY + summaryH + gap;
            int workH = Math.Max(220, statusY - workY - gap);

            int detailsW = Math.Max(350, (int)(contentW * 0.30));
            int resultsW = contentW - detailsW - gap;

            pnlResults.SetBounds(margin, workY, resultsW, workH);
            pnlDetails.SetBounds(margin + resultsW + gap, workY, detailsW, workH);

            LayoutResults();
            LayoutDetails();
        }

        private void LayoutToolbarButtons(int x, int y)
        {
            int bw = 118;
            int bh = 32;
            int g = 8;

            btnScan.SetBounds(x, y, bw, bh);
            btnAnalyze.SetBounds(x + (bw + g), y, bw, bh);
            btnCopy.SetBounds(x + (bw + g) * 2, y, bw, bh);
            btnVerify.SetBounds(x + (bw + g) * 3, y, bw, bh);
            btnReport.SetBounds(x + (bw + g) * 4, y, bw, bh);
        }

        private void LayoutSourceCard()
        {
            int pW = pnlSource.ClientSize.Width;
            int pH = pnlSource.ClientSize.Height;
            int m = 16;

            lblSourceTitle.Location = new Point(m, 12);
            lblSourceSub.Location = new Point(m, 36);

            int btnTop = 14;
            int clearW = 82;
            int removeW = 122;
            int addW = 126;
            int gap = 8;
            int startX = pW - m - addW - gap - removeW - gap - clearW;

            btnAddSource.SetBounds(startX, btnTop, addW, 32);
            btnRemoveSource.SetBounds(startX + addW + gap, btnTop, removeW, 32);
            btnClearSources.SetBounds(startX + addW + gap + removeW + gap, btnTop, clearW, 32);

            int listTop = 62;
            int listHeight = Math.Max(58, pH - listTop - 14);
            lstSources.SetBounds(m, listTop, pW - (m * 2), listHeight);
        }

        private void LayoutTargetCard()
        {
            int pW = pnlTarget.ClientSize.Width;
            int m = 16;

            lblTargetTitle.Location = new Point(m, 12);
            lblTargetSub.Location = new Point(m, 36);

            int topBtnW = 120;
            int topBtnGap = 8;
            int openBtnW = 104;
            int topBtnY = 14;

            btnOpenTarget.SetBounds(pW - m - openBtnW, topBtnY, openBtnW, 32);
            btnChangeTarget.SetBounds(pW - m - openBtnW - topBtnGap - topBtnW, topBtnY, topBtnW, 32);

            Control lblTarget = pnlTarget.Controls["lblTargetFolder"];
            lblTarget.Location = new Point(m, 72);

            int browseW = 42;
            int targetX = 120;
            int targetY = 68;

            txtTarget.SetBounds(targetX, targetY, pW - targetX - browseW - (m * 3), 25);
            btnBrowseTarget.SetBounds(pW - m - browseW, targetY - 1, browseW, 27);

            lblSafety.Location = new Point(m, 106);
        }

        private void LayoutSummary()
        {
            int m = 10;
            int count = 5;
            int itemW = (pnlSummary.ClientSize.Width - (m * 2)) / count;
            int x = m;

            foreach (MetricCard card in new[] { metricTotalFiles, metricToArchive, metricDuplicates, metricConflicts, metricVerified })
            {
                card.SetBounds(x, 7, itemW - 8, pnlSummary.ClientSize.Height - 14);
                x += itemW;
            }
        }

        private void LayoutResults()
        {
            int w = pnlResults.ClientSize.Width;
            int h = pnlResults.ClientSize.Height;
            int m = 16;

            Control title = pnlResults.Controls["lblResultsTitle"];
            Control sub = pnlResults.Controls["lblResultsSub"];
            Control filter = cmbDecisionFilter;
            Control search = txtSearch;

            title.Location = new Point(m, 12);
            sub.Location = new Point(m, 38);

            search.SetBounds(w - m - 220, 16, 220, 28);
            filter.SetBounds(w - m - 220 - 12 - 150, 16, 150, 28);

            gridResults.SetBounds(m, 62, w - (m * 2), h - 78);
        }

        private void LayoutDetails()
        {
            int w = pnlDetails.ClientSize.Width;
            int h = pnlDetails.ClientSize.Height;
            int m = 16;

            Control title = pnlDetails.Controls["lblDetailsTitle"];
            Control sub = pnlDetails.Controls["lblDetailsSub"];

            title.Location = new Point(m, 12);
            sub.Location = new Point(m, 38);

            txtDetails.SetBounds(m, 62, w - (m * 2), h - 78);
        }

        private Panel CreateCard()
        {
            Panel panel = new()
            {
                BackColor = _cardBack,
                BorderStyle = BorderStyle.FixedSingle
            };

            return panel;
        }

        private Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = _text
            };
        }

        private Label CreateSmallText(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = _muted
            };
        }

        private Button CreateToolbarButton(string text, ToolbarIcon icon)
        {
            Button button = new()
            {
                Text = text,
                Tag = icon,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = _text,
                UseVisualStyleBackColor = false,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(12, 0, 12, 0),
                Image = CreateToolbarIcon(icon, _muted)
            };

            button.FlatAppearance.BorderColor = _border;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 244, 252);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 237, 251);

            return button;
        }

        private Button CreateHeaderButton(string text)
        {
            Button button = new()
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                BackColor = _pageBack,
                ForeColor = _text,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 239, 249);

            return button;
        }

        private Button CreateSecondaryButton(string text)
        {
            Button button = new()
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = _text,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = _border;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 244, 252);

            return button;
        }

        private Button CreateDarkButton(string text)
        {
            Button button = new()
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(64, 78, 99),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(54, 68, 89);

            return button;
        }

        private void PaintBottomBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel)
            {
                return;
            }

            using Pen pen = new(_border, 1);
            e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
        }

        private void WireUiEvents()
        {
            btnAddSource.Click += BtnAddSource_Click;
            btnRemoveSource.Click += BtnRemoveSource_Click;
            btnClearSources.Click += BtnClearSources_Click;
            btnChangeTarget.Click += BtnSelectTarget_Click;
            btnBrowseTarget.Click += BtnSelectTarget_Click;
            btnOpenTarget.Click += BtnOpenTarget_Click;
            btnOptions.Click += BtnOptions_Click;

            btnScan.Click += BtnScan_Click;
            btnAnalyze.Click += BtnAnalyze_Click;
            btnCopy.Click += BtnCopy_Click;
            btnVerify.Click += BtnVerify_Click;
            btnReport.Click += BtnReport_Click;

            lstSources.SelectedIndexChanged += (_, _) => UpdateUiState();
            cmbDecisionFilter.SelectedIndexChanged += (_, _) => RefreshCurrentGrid();
            txtSearch.TextChanged += (_, _) => RefreshCurrentGrid();
            gridResults.SelectionChanged += GridResults_SelectionChanged;
        }

        private void UpdateUiState()
        {
            bool hasSources = lstSources.Items.Count > 0;
            bool hasSelection = lstSources.SelectedItems.Count > 0;
            bool hasTarget = !string.IsNullOrWhiteSpace(txtTarget.Text);
            bool hasScan = _scannedFiles.Count > 0;
            bool hasAnalysis = _groups.Count > 0;
            bool hasCopy = _copyResultRecords.Any(r => r.Success && !r.IsConflictVaultCopy);
            bool hasVerify = _verificationResultsByRelativePath.Count > 0;

            lblSourceSub.Text = hasSources
                ? $"{lstSources.Items.Count:N0} source root(s) selected."
                : "No source folders selected.";

            lblTargetSub.Text = hasTarget
                ? txtTarget.Text
                : "Target folder not selected.";

            btnAddSource.Enabled = !_isBusy;
            btnRemoveSource.Enabled = !_isBusy && hasSelection;
            btnClearSources.Enabled = !_isBusy && hasSources;
            btnChangeTarget.Enabled = !_isBusy;
            btnBrowseTarget.Enabled = !_isBusy;
            btnOpenTarget.Enabled = !_isBusy && hasTarget && Directory.Exists(txtTarget.Text);

            ApplyToolbarState(btnScan, !_isBusy && hasSources && hasTarget, hasSources && hasTarget && !hasScan, hasScan);
            ApplyToolbarState(btnAnalyze, !_isBusy && hasScan, hasScan && !hasAnalysis, hasAnalysis);
            ApplyToolbarState(btnCopy, !_isBusy && hasAnalysis, hasAnalysis && !hasCopy, hasCopy);
            ApplyToolbarState(btnVerify, !_isBusy && hasCopy, hasCopy && !hasVerify, hasVerify);
            ApplyToolbarState(btnReport, !_isBusy && hasVerify, hasVerify, false);

            UpdateTargetSafetyStatus();
        }

        private void ApplyToolbarState(Button button, bool enabled, bool active, bool completed)
        {
            button.Enabled = enabled;
            button.Text = button.Text.Replace("✓ ", string.Empty);

            if (completed)
            {
                button.Text = "✓ " + button.Text;
                button.BackColor = Color.FromArgb(238, 248, 242);
                button.ForeColor = _green;
                button.FlatAppearance.BorderColor = Color.FromArgb(190, 224, 203);
                RefreshToolbarIcon(button, _green);
                return;
            }

            if (active)
            {
                button.BackColor = Color.FromArgb(232, 242, 255);
                button.ForeColor = _blue;
                button.FlatAppearance.BorderColor = _blue;
                RefreshToolbarIcon(button, _blue);
                return;
            }

            button.BackColor = Color.White;
            button.ForeColor = enabled ? _text : _muted;
            button.FlatAppearance.BorderColor = _border;
            RefreshToolbarIcon(button, enabled ? _muted : Color.FromArgb(150, 158, 170));
        }

        private void SetBusy(bool busy, string title, string message)
        {
            _isBusy = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            SetStatus(title, message);
            UpdateUiState();
            System.Windows.Forms.Application.DoEvents();
        }

        private void SetStatus(string title, string message, bool isError = false)
        {
            lblStatusTitle.Text = title;
            lblStatusTitle.ForeColor = isError ? Color.FromArgb(175, 45, 45) : _green;
            lblStatusMessage.Text = message;
        }

        private void UpdateTargetSafetyStatus()
        {
            List<string> sourceRoots = GetSourceFolders();
            string target = txtTarget.Text.Trim();

            if (sourceRoots.Count == 0 || string.IsNullOrWhiteSpace(target))
            {
                lblSafety.Text = "● Target Safety: Not checked yet";
                lblSafety.ForeColor = _muted;
                return;
            }

            TargetPreflightResult result = _targetPreflightService.ValidateNewArchiveTarget(sourceRoots, target);

            if (result.IsValid)
            {
                lblSafety.Text = "● Target Safe";
                lblSafety.ForeColor = _green;
            }
            else
            {
                lblSafety.Text = "● Target Blocked";
                lblSafety.ForeColor = Color.FromArgb(175, 45, 45);
            }
        }

        private List<string> GetSourceFolders()
        {
            return lstSources.Items.Cast<string>().ToList();
        }

        private void ResetWorkflowData()
        {
            _scannedFiles.Clear();
            _groups.Clear();
            _copyResultsByRelativePath.Clear();
            _copyResultRecords.Clear();
            _verificationResultsByRelativePath.Clear();
            _resultsMode = AppResultsMode.Scan;
            gridResults.Rows.Clear();
            txtDetails.Text = "Decision details will appear here.";
            UpdateMetrics(0, 0, 0, 0, 0);
        }

        private void UpdateMetrics(int totalFiles, int toArchiveFiles, int duplicateFilesSkipped, int conflictGroups, int verifiedOk)
        {
            metricTotalFiles.SetValue(totalFiles.ToString("N0"));
            metricToArchive.SetValue(toArchiveFiles.ToString("N0"));
            metricDuplicates.SetValue(duplicateFilesSkipped.ToString("N0"));
            metricConflicts.SetValue(conflictGroups.ToString("N0"));
            metricVerified.SetValue(verifiedOk.ToString("N0"));
        }

        private void BtnAddSource_Click(object? sender, EventArgs e)
        {
            List<string> selectedFolders = AppMultiFolderPicker.ShowDialog(
                Handle,
                "Select one or more source root folders. Use Ctrl or Shift to select multiple folders.");

            if (selectedFolders.Count == 0)
                return;

            int added = 0;
            foreach (string folder in selectedFolders)
            {
                string normalized = NormalizeDirectoryPath(folder);
                bool exists = lstSources.Items.Cast<string>().Any(x =>
                    string.Equals(NormalizeDirectoryPath(x), normalized, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    lstSources.Items.Add(normalized);
                    added++;
                }
            }

            ResetWorkflowData();
            SetStatus("Source folders updated", $"Added: {added:N0}. Total source roots: {lstSources.Items.Count:N0}.");
            UpdateUiState();
        }

        private void BtnRemoveSource_Click(object? sender, EventArgs e)
        {
            if (lstSources.SelectedItems.Count == 0)
                return;

            while (lstSources.SelectedItems.Count > 0)
                lstSources.Items.Remove(lstSources.SelectedItems[0]);

            ResetWorkflowData();
            SetStatus("Source folders updated", $"Source roots remaining: {lstSources.Items.Count:N0}.");
            UpdateUiState();
        }

        private void BtnClearSources_Click(object? sender, EventArgs e)
        {
            lstSources.Items.Clear();
            ResetWorkflowData();
            SetStatus("Source folders cleared", "Select source folders to begin.");
            UpdateUiState();
        }

        private void BtnSelectTarget_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = "Select the target archive folder. FileForge V1 requires an empty target folder.",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            txtTarget.Text = NormalizeDirectoryPath(dialog.SelectedPath);
            _copyResultsByRelativePath.Clear();
            _copyResultRecords.Clear();
            _verificationResultsByRelativePath.Clear();
            SetStatus("Target selected", txtTarget.Text);
            UpdateUiState();
        }

        private void BtnOpenTarget_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTarget.Text) || !Directory.Exists(txtTarget.Text))
            {
                SetStatus("Target unavailable", "Select a valid target folder first.", true);
                return;
            }

            OpenFile(txtTarget.Text);
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            if (lstSources.Items.Count == 0)
            {
                MessageBox.Show("Please add at least one source folder.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTarget.Text))
            {
                MessageBox.Show("Please select an archive target folder.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                SetBusy(true, "Scanning", "Scanning source folders...");
                ResetWorkflowData();

                List<string> sourceFolders = GetSourceFolders();
                List<SourceFileRecord> scanned = await Task.Run(() => _folderScanService.ScanFolders(sourceFolders));

                _scannedFiles.Clear();
                _scannedFiles.AddRange(scanned);
                _resultsMode = AppResultsMode.Scan;

                RefreshCurrentGrid();
                UpdateMetrics(_scannedFiles.Count, 0, 0, 0, 0);
                SetStatus("Scan complete", $"Files found: {_scannedFiles.Count:N0}.");
            }
            catch (Exception ex)
            {
                SetStatus("Scan failed", ex.Message, true);
                MessageBox.Show(ex.Message, "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, lblStatusTitle.Text, lblStatusMessage.Text);
            }
        }

        private async void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            if (_scannedFiles.Count == 0)
            {
                MessageBox.Show("Please run Scan before Analyze.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                SetBusy(true, "Analyzing", "Hashing duplicate-path candidates and building archive decisions...");

                int hashedCount = await Task.Run(() => _fileHashService.CalculateRequiredHashes(_scannedFiles));
                _groups.Clear();
                _groups.AddRange(BuildGroups(_scannedFiles, GetSourceFolders()));
                _copyResultsByRelativePath.Clear();
                _copyResultRecords.Clear();
                _verificationResultsByRelativePath.Clear();
                _resultsMode = AppResultsMode.Analysis;

                RefreshCurrentGrid();
                AppArchiveStatistics stats = CalculateArchiveStatistics();
                UpdateMetrics(_scannedFiles.Count, stats.ToArchiveFiles, stats.DuplicateFilesSkipped, stats.ConflictGroups, 0);

                SetStatus("Analyze complete", $"To archive: {stats.ToArchiveFiles:N0}. Duplicate files skipped: {stats.DuplicateFilesSkipped:N0}. Conflicts auto-resolved: {stats.ConflictGroups:N0}. Files hashed: {hashedCount:N0}.");
            }
            catch (Exception ex)
            {
                SetStatus("Analyze failed", ex.Message, true);
                MessageBox.Show(ex.Message, "Analyze Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, lblStatusTitle.Text, lblStatusMessage.Text);
            }
        }

        private async void BtnCopy_Click(object? sender, EventArgs e)
        {
            if (_groups.Count == 0)
            {
                MessageBox.Show("Please run Analyze before Copy.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetRoot = txtTarget.Text.Trim();
            List<string> sourceRoots = GetSourceFolders();
            TargetPreflightResult preflight = _targetPreflightService.ValidateNewArchiveTarget(sourceRoots, targetRoot);

            if (!preflight.IsValid)
            {
                SetStatus("Copy blocked", preflight.Message, true);
                MessageBox.Show(preflight.Message, "Target Safety Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateUiState();
                return;
            }

            List<ConsolidationGroup> copyableGroups = _groups.Where(IsMainArchiveCandidate).ToList();
            if (copyableGroups.Count == 0)
            {
                SetStatus("Copy blocked", "No copyable archive files found.", true);
                return;
            }

            AppArchiveStatistics stats = CalculateArchiveStatistics();
            DialogResult confirm = MessageBox.Show(
                $"Copy {copyableGroups.Count:N0} main archive file(s) to the target folder?\n\n" +
                $"Duplicate files skipped: {stats.DuplicateFilesSkipped:N0}\n" +
                $"Conflicting older versions preserved in vault: {stats.ConflictVaultFiles:N0}\n" +
                $"Target: {targetRoot}",
                "Confirm Copy",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                SetStatus("Copy cancelled", "No files were copied.");
                return;
            }

            try
            {
                SetBusy(true, "Copying", "Copying selected archive files...");
                _copyResultsByRelativePath.Clear();
                _copyResultRecords.Clear();
                _verificationResultsByRelativePath.Clear();

                Directory.CreateDirectory(targetRoot);
                int emptyDirectoriesCreated = 0;

                if (_preserveEmptyDirectories)
                    emptyDirectoriesCreated = await Task.Run(() => CreateEmptyDirectoryStructure(sourceRoots, targetRoot));

                int copied = 0;
                int failed = 0;
                int totalWork = copyableGroups.Count + stats.ConflictVaultFiles;
                int workDone = 0;

                foreach (ConsolidationGroup group in copyableGroups)
                {
                    SourceFileRecord selected = group.SelectedFile!;
                    string destinationFile = Path.Combine(targetRoot, group.RelativePath);
                    SetStatus("Copying", $"Copying {workDone + 1:N0}/{Math.Max(1, totalWork):N0}: {group.RelativePath}");

                    AppCopyResult mainResult = await Task.Run(() => CopyOneFile(group.RelativePath, selected.FullPath, destinationFile));
                    _copyResultsByRelativePath[group.RelativePath] = mainResult;
                    _copyResultRecords.Add(mainResult);
                    workDone++;

                    if (mainResult.Success) copied++; else failed++;

                    if (group.Status == ConsolidationStatus.ConflictDifferentContent)
                    {
                        int vaultIndex = 0;
                        foreach (SourceFileRecord older in group.Files
                                     .Where(f => !string.Equals(f.FullPath, selected.FullPath, StringComparison.OrdinalIgnoreCase))
                                     .OrderByDescending(f => f.LastModifiedTime)
                                     .ThenByDescending(f => f.SizeBytes))
                        {
                            vaultIndex++;
                            string vaultPath = BuildConflictVaultDestination(targetRoot, group.RelativePath, older, sourceRoots, vaultIndex);
                            SetStatus("Copying", $"Preserving conflict version {workDone + 1:N0}/{Math.Max(1, totalWork):N0}: {group.RelativePath}");

                            AppCopyResult vaultResult = await Task.Run(() => CopyOneFile(
                                BuildVaultRelativePath(targetRoot, vaultPath),
                                older.FullPath,
                                vaultPath,
                                group.RelativePath,
                                "Conflict Vault"));

                            _copyResultRecords.Add(vaultResult);
                            workDone++;

                            if (vaultResult.Success) copied++; else failed++;
                        }
                    }
                }

                _resultsMode = AppResultsMode.Copy;
                RefreshCurrentGrid();
                UpdateMetrics(_scannedFiles.Count, stats.ToArchiveFiles, stats.DuplicateFilesSkipped, stats.ConflictGroups, 0);

                string emptyText = _preserveEmptyDirectories ? $" Empty folders recreated: {emptyDirectoriesCreated:N0}." : string.Empty;
                SetStatus(failed == 0 ? "Copy complete" : "Copy completed with errors", $"Copied/preserved: {copied:N0}. Failed: {failed:N0}.{emptyText}", failed > 0);

                if (_openTargetFolderAfterCopy)
                    OpenFile(targetRoot);
            }
            catch (Exception ex)
            {
                SetStatus("Copy failed", ex.Message, true);
                MessageBox.Show(ex.Message, "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, lblStatusTitle.Text, lblStatusMessage.Text);
            }
        }

        private async void BtnVerify_Click(object? sender, EventArgs e)
        {
            if (_groups.Count == 0)
            {
                MessageBox.Show("Please run Analyze before Verify.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetRoot = txtTarget.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetRoot) || !Directory.Exists(targetRoot))
            {
                MessageBox.Show("Please select a valid target folder before Verify.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<ConsolidationGroup> expectedGroups = _groups.Where(IsMainArchiveCandidate).ToList();
            if (expectedGroups.Count == 0)
            {
                SetStatus("Verify blocked", "No expected archive files found.", true);
                return;
            }

            try
            {
                SetBusy(true, "Verifying", "Checking copied file existence, size, and SHA256 hash...");
                await Task.Run(() => EnsureSelectedHashes(expectedGroups));

                List<CopyVerificationResult> results = await Task.Run(() =>
                    _copyVerificationService.VerifyCopiedFiles(expectedGroups, targetRoot));

                _verificationResultsByRelativePath.Clear();
                foreach (CopyVerificationResult result in results)
                    _verificationResultsByRelativePath[result.RelativePath] = result;

                _resultsMode = AppResultsMode.Verify;
                RefreshCurrentGrid();

                AppArchiveStatistics stats = CalculateArchiveStatistics();
                int verified = results.Count(r => r.IsVerified);
                int failed = results.Count - verified;
                UpdateMetrics(_scannedFiles.Count, stats.ToArchiveFiles, stats.DuplicateFilesSkipped, stats.ConflictGroups, verified);

                SetStatus(failed == 0 ? "Verify complete" : "Verify completed with failures", $"Verified: {verified:N0}. Failed: {failed:N0}.", failed > 0);
            }
            catch (Exception ex)
            {
                SetStatus("Verify failed", ex.Message, true);
                MessageBox.Show(ex.Message, "Verify Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, lblStatusTitle.Text, lblStatusMessage.Text);
            }
        }

        private void BtnReport_Click(object? sender, EventArgs e)
        {
            if (_scannedFiles.Count == 0 || _groups.Count == 0)
            {
                MessageBox.Show("Please run Scan and Analyze before Report.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_copyResultRecords.Count == 0)
            {
                MessageBox.Show("Please run Copy before Report.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                AppArchiveStatistics stats = CalculateArchiveStatistics();
                List<CopyVerificationResult> verificationResults = _verificationResultsByRelativePath.Values
                    .OrderBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                AuditReportRequest request = new()
                {
                    GeneratedAt = DateTime.Now,
                    ApplicationName = "FileForge",
                    ApplicationMode = "New Archive Mode",
                    HashAlgorithm = "SHA256",
                    TargetSafetyPolicy = "Target must be empty before Copy. No overwrite. Conflicts auto-resolved to latest main archive plus older versions in _FileForge_Conflicts.",
                    SourceRoots = GetSourceFolders(),
                    TargetRoot = txtTarget.Text.Trim(),
                    PreserveEmptyDirectories = _preserveEmptyDirectories,
                    TotalFiles = _scannedFiles.Count,
                    UniqueGroups = stats.UniqueGroups,
                    DuplicateGroups = stats.DuplicateGroups,
                    ToArchiveFiles = stats.ToArchiveFiles,
                    DuplicateFilesSkipped = stats.DuplicateFilesSkipped,
                    ConflictGroups = stats.ConflictGroups,
                    ScannedFiles = _scannedFiles.ToList(),
                    Groups = _groups.ToList(),
                    CopyRecords = _copyResultRecords.Select(r => new AuditCopyRecord
                    {
                        RelativePath = r.RelativePath,
                        SourcePath = r.SourcePath,
                        DestinationPath = r.DestinationPath,
                        Success = r.Success,
                        Skipped = r.Skipped,
                        Message = r.Message,
                        BytesCopied = r.BytesCopied
                    }).ToList(),
                    VerificationResults = verificationResults,
                    IncludeFullSourcePaths = _includeFullSourcePathsInReport
                };

                AuditReportResult result = _reportService.GenerateHtmlReport(request);
                SetStatus("Report generated", result.HtmlReportPath);

                MessageBox.Show("Audit report generated successfully.\n\n" + result.HtmlReportPath, "Report Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_openAuditReportAfterGeneration)
                    OpenFile(result.HtmlReportPath);
            }
            catch (Exception ex)
            {
                SetStatus("Report failed", ex.Message, true);
                MessageBox.Show(ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UpdateUiState();
            }
        }

        private void BtnOptions_Click(object? sender, EventArgs e)
        {
            using Form dialog = new()
            {
                Text = "FileForge Options",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(520, 270),
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            Label title = new()
            {
                Text = "Archive Options",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = _text,
                Location = new Point(22, 18),
                AutoSize = true
            };

            CheckBox chkPreserve = new()
            {
                Text = "Preserve Empty Directories",
                Checked = _preserveEmptyDirectories,
                Location = new Point(24, 60),
                Size = new Size(430, 26)
            };

            CheckBox chkOpenTarget = new()
            {
                Text = "Open target folder after successful Copy",
                Checked = _openTargetFolderAfterCopy,
                Location = new Point(24, 92),
                Size = new Size(430, 26)
            };

            CheckBox chkOpenReport = new()
            {
                Text = "Open HTML audit report after generation",
                Checked = _openAuditReportAfterGeneration,
                Location = new Point(24, 124),
                Size = new Size(430, 26)
            };

            CheckBox chkFullPaths = new()
            {
                Text = "Include full source paths in audit report",
                Checked = _includeFullSourcePathsInReport,
                Location = new Point(24, 156),
                Size = new Size(430, 26)
            };

            Label mode = new()
            {
                Text = "Target Mode: New Archive Mode — target must be empty before Copy.",
                Location = new Point(24, 190),
                Size = new Size(460, 26),
                ForeColor = _muted
            };

            Button ok = CreateSecondaryButton("OK");
            ok.DialogResult = DialogResult.OK;
            ok.SetBounds(314, 226, 80, 30);

            Button cancel = CreateSecondaryButton("Cancel");
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(404, 226, 90, 30);

            dialog.Controls.AddRange(new Control[] { title, chkPreserve, chkOpenTarget, chkOpenReport, chkFullPaths, mode, ok, cancel });
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _preserveEmptyDirectories = chkPreserve.Checked;
            _openTargetFolderAfterCopy = chkOpenTarget.Checked;
            _openAuditReportAfterGeneration = chkOpenReport.Checked;
            _includeFullSourcePathsInReport = chkFullPaths.Checked;

            SetStatus("Options updated", _preserveEmptyDirectories ? "Mode: Full Folder Reconstruction." : "Mode: Backup Archive.");
            UpdateUiState();
        }

        private void RefreshToolbarIcon(Button button, Color color)
        {
            if (button.Tag is ToolbarIcon icon)
            {
                button.Image = CreateToolbarIcon(icon, color);
            }
        }

        private Bitmap CreateToolbarIcon(ToolbarIcon icon, Color color)
        {
            Bitmap bmp = new(22, 22);

            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using Pen pen = new(color, 2F)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };

            using Pen thin = new(color, 1.6F)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };

            using Brush brush = new SolidBrush(color);

            switch (icon)
            {
                case ToolbarIcon.Scan:
                    g.DrawEllipse(pen, 4, 4, 9, 9);
                    g.DrawLine(pen, 12, 12, 18, 18);
                    break;

                case ToolbarIcon.Analyze:
                    g.DrawRectangle(thin, 6, 3, 10, 16);
                    g.DrawLine(thin, 8, 8, 14, 8);
                    g.DrawLine(thin, 8, 12, 14, 12);
                    g.DrawLine(thin, 8, 16, 12, 16);
                    break;

                case ToolbarIcon.Copy:
                    g.DrawRectangle(thin, 5, 7, 10, 11);
                    g.DrawRectangle(thin, 8, 4, 10, 11);
                    break;

                case ToolbarIcon.Verify:
                    Point[] shield =
                    {
                        new(11, 3),
                        new(17, 6),
                        new(16, 13),
                        new(11, 19),
                        new(6, 13),
                        new(5, 6)
                    };
                    g.DrawPolygon(pen, shield);
                    g.DrawLine(pen, 8, 11, 10, 14);
                    g.DrawLine(pen, 10, 14, 15, 8);
                    break;

                case ToolbarIcon.Report:
                    g.DrawRectangle(thin, 6, 3, 11, 16);
                    g.DrawLine(thin, 8, 8, 15, 8);
                    g.DrawLine(thin, 8, 12, 15, 12);
                    g.DrawLine(thin, 8, 16, 13, 16);
                    g.FillEllipse(brush, 15, 4, 3, 3);
                    break;
            }

            return bmp;
        }

        private void RefreshCurrentGrid()
        {
            switch (_resultsMode)
            {
                case AppResultsMode.Analysis:
                    PopulateAnalysisGrid();
                    break;
                case AppResultsMode.Copy:
                    PopulateCopyGrid();
                    break;
                case AppResultsMode.Verify:
                    PopulateVerificationGrid();
                    break;
                default:
                    PopulateScanGrid();
                    break;
            }
        }

        private void PopulateScanGrid()
        {
            gridResults.Rows.Clear();
            int i = 0;

            foreach (SourceFileRecord file in FilterFiles(_scannedFiles.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)))
            {
                i++;
                int row = gridResults.Rows.Add(
                    i.ToString("N0"),
                    file.RelativePath,
                    "Scanned",
                    file.SourceRoot,
                    file.FullPath,
                    FormatBytes(file.SizeBytes),
                    file.LastModifiedTime.ToString("yyyy-MM-dd HH:mm"));
                gridResults.Rows[row].Tag = file.RelativePath;
            }

            SelectFirstGridRow();
        }

        private void PopulateAnalysisGrid()
        {
            gridResults.Rows.Clear();
            int i = 0;

            foreach (ConsolidationGroup group in FilterGroups(_groups))
            {
                i++;
                SourceFileRecord? selected = group.SelectedFile;
                string otherInfo = BuildOtherInfo(group);

                int row = gridResults.Rows.Add(
                    i.ToString("N0"),
                    group.RelativePath,
                    FormatStatus(group.Status),
                    selected?.FullPath ?? string.Empty,
                    otherInfo,
                    selected == null ? string.Empty : FormatBytes(selected.SizeBytes),
                    selected == null ? string.Empty : selected.LastModifiedTime.ToString("yyyy-MM-dd HH:mm"));

                gridResults.Rows[row].Tag = group.RelativePath;
                gridResults.Rows[row].DefaultCellStyle.ForeColor = StatusColor(group.Status);
            }

            SelectFirstGridRow();
        }

        private void PopulateCopyGrid()
        {
            gridResults.Rows.Clear();
            int i = 0;

            foreach (ConsolidationGroup group in FilterGroups(_groups))
            {
                i++;
                _copyResultsByRelativePath.TryGetValue(group.RelativePath, out AppCopyResult? copyResult);
                SourceFileRecord? selected = group.SelectedFile;

                string decision = copyResult == null
                    ? FormatStatus(group.Status)
                    : copyResult.Success ? "Copied" : copyResult.Skipped ? "Skipped" : "Copy Failed";

                int row = gridResults.Rows.Add(
                    i.ToString("N0"),
                    group.RelativePath,
                    decision,
                    selected?.FullPath ?? string.Empty,
                    copyResult?.Message ?? BuildOtherInfo(group),
                    selected == null ? string.Empty : FormatBytes(selected.SizeBytes),
                    selected == null ? string.Empty : selected.LastModifiedTime.ToString("yyyy-MM-dd HH:mm"));

                gridResults.Rows[row].Tag = group.RelativePath;
                gridResults.Rows[row].DefaultCellStyle.ForeColor = copyResult == null || copyResult.Success
                    ? StatusColor(group.Status)
                    : Color.FromArgb(175, 45, 45);
            }

            SelectFirstGridRow();
        }

        private void PopulateVerificationGrid()
        {
            gridResults.Rows.Clear();
            int i = 0;

            foreach (ConsolidationGroup group in FilterGroups(_groups))
            {
                i++;
                _verificationResultsByRelativePath.TryGetValue(group.RelativePath, out CopyVerificationResult? verification);
                SourceFileRecord? selected = group.SelectedFile;

                string decision = verification == null
                    ? "Not Verified"
                    : verification.IsVerified ? "Verified" : "Verification Failed";

                int row = gridResults.Rows.Add(
                    i.ToString("N0"),
                    group.RelativePath,
                    decision,
                    selected?.FullPath ?? string.Empty,
                    verification?.Message ?? BuildOtherInfo(group),
                    selected == null ? string.Empty : FormatBytes(selected.SizeBytes),
                    selected == null ? string.Empty : selected.LastModifiedTime.ToString("yyyy-MM-dd HH:mm"));

                gridResults.Rows[row].Tag = group.RelativePath;
                gridResults.Rows[row].DefaultCellStyle.ForeColor = verification == null || verification.IsVerified
                    ? StatusColor(group.Status)
                    : Color.FromArgb(175, 45, 45);
            }

            SelectFirstGridRow();
        }

        private IEnumerable<SourceFileRecord> FilterFiles(IEnumerable<SourceFileRecord> files)
        {
            string search = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return files;

            return files.Where(f =>
                f.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                f.FullPath.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<ConsolidationGroup> FilterGroups(IEnumerable<ConsolidationGroup> groups)
        {
            IEnumerable<ConsolidationGroup> result = groups.OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase);
            string filter = cmbDecisionFilter.SelectedItem?.ToString() ?? "All Decisions";

            result = filter switch
            {
                "Unique" => result.Where(g => g.Status == ConsolidationStatus.Unique),
                "Duplicates" => result.Where(g => g.Status == ConsolidationStatus.DuplicateSameContent),
                "Conflicts" => result.Where(g => g.Status == ConsolidationStatus.ConflictDifferentContent || g.Status == ConsolidationStatus.Error),
                "Verified" => result.Where(g => _verificationResultsByRelativePath.TryGetValue(g.RelativePath, out CopyVerificationResult? v) && v.IsVerified),
                "Failed" => result.Where(g => _verificationResultsByRelativePath.TryGetValue(g.RelativePath, out CopyVerificationResult? v) && !v.IsVerified),
                _ => result
            };

            string search = txtSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(g =>
                    g.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    g.Files.Any(f => f.FullPath.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            return result;
        }

        private void SelectFirstGridRow()
        {
            if (gridResults.Rows.Count > 0)
            {
                gridResults.ClearSelection();
                gridResults.Rows[0].Selected = true;
            }
            else
            {
                txtDetails.Text = "No rows to display.";
            }
        }

        private void GridResults_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridResults.SelectedRows.Count == 0)
                return;

            string relativePath = gridResults.SelectedRows[0].Tag?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(relativePath))
                relativePath = gridResults.SelectedRows[0].Cells["colRelativePath"].Value?.ToString() ?? string.Empty;

            if (_resultsMode == AppResultsMode.Scan)
            {
                SourceFileRecord? file = _scannedFiles.FirstOrDefault(f => string.Equals(f.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
                txtDetails.Text = file == null ? "Select a scanned file." : BuildScanDetails(file);
                return;
            }

            ConsolidationGroup? group = _groups.FirstOrDefault(g => string.Equals(g.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
            if (group == null)
            {
                txtDetails.Text = "Select a decision row.";
                return;
            }

            if (_resultsMode == AppResultsMode.Copy)
            {
                _copyResultsByRelativePath.TryGetValue(relativePath, out AppCopyResult? copyResult);
                txtDetails.Text = BuildCopyDetails(group, copyResult);
                return;
            }

            if (_resultsMode == AppResultsMode.Verify)
            {
                _verificationResultsByRelativePath.TryGetValue(relativePath, out CopyVerificationResult? verifyResult);
                txtDetails.Text = BuildVerificationDetails(group, verifyResult);
                return;
            }

            txtDetails.Text = BuildAnalysisDetails(group);
        }

        private static List<ConsolidationGroup> BuildGroups(List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
        {
            return files
                .GroupBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildGroup(g.Key, g.ToList(), sourceRootOrder))
                .OrderBy(g => g.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static ConsolidationGroup BuildGroup(string relativePath, List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
        {
            ConsolidationGroup group = new()
            {
                RelativePath = relativePath,
                Files = files
            };

            if (files.Count == 1)
            {
                group.SelectedFile = files[0];
                group.Status = ConsolidationStatus.Unique;
                group.DecisionReason = "Only one file found at this relative path. Hash not required for uniqueness.";
                return group;
            }

            int distinctSizeCount = files.Select(f => f.SizeBytes).Distinct().Count();
            if (distinctSizeCount > 1)
            {
                group.SelectedFile = SelectLatestModifiedCandidate(files, sourceRootOrder);
                group.Status = ConsolidationStatus.ConflictDifferentContent;
                group.DecisionReason = "Conflict auto-resolved. Main archive version selected by latest modified date. Older-dated conflicting versions preserved under _FileForge_Conflicts during Copy. File sizes differ, so content differs; hash not required.";
                return group;
            }

            if (files.Any(f => !f.HashCalculated))
            {
                group.SelectedFile = SelectLatestModifiedCandidate(files, sourceRootOrder);
                group.Status = ConsolidationStatus.Error;
                group.DecisionReason = "One or more same-size files with the same relative path could not be hashed. Copy is blocked for this group until the file read/hash issue is resolved.";
                return group;
            }

            int distinctHashCount = files.Select(f => f.Sha256Hash).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            if (distinctHashCount == 1)
            {
                group.SelectedFile = SelectFirstSourceCandidate(files, sourceRootOrder);
                group.Status = ConsolidationStatus.DuplicateSameContent;
                group.DecisionReason = "Same relative path, same file size, and same SHA256 content hash. First selected source root wins; other copies are skipped.";
                return group;
            }

            group.SelectedFile = SelectLatestModifiedCandidate(files, sourceRootOrder);
            group.Status = ConsolidationStatus.ConflictDifferentContent;
            group.DecisionReason = "Conflict auto-resolved. Main archive version selected by latest modified date. Older-dated conflicting versions preserved under _FileForge_Conflicts during Copy. Same relative path and same file size, but different SHA256 content hashes.";
            return group;
        }

        private static SourceFileRecord SelectFirstSourceCandidate(List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
        {
            return files
                .OrderBy(f => SourceOrderIndex(f.SourceRoot, sourceRootOrder))
                .ThenByDescending(f => f.LastModifiedTime)
                .ThenByDescending(f => f.SizeBytes)
                .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static SourceFileRecord SelectLatestModifiedCandidate(List<SourceFileRecord> files, IReadOnlyList<string> sourceRootOrder)
        {
            return files
                .OrderByDescending(f => f.LastModifiedTime)
                .ThenByDescending(f => f.SizeBytes)
                .ThenBy(f => SourceOrderIndex(f.SourceRoot, sourceRootOrder))
                .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static int SourceOrderIndex(string sourceRoot, IReadOnlyList<string> sourceRootOrder)
        {
            for (int i = 0; i < sourceRootOrder.Count; i++)
            {
                if (string.Equals(sourceRootOrder[i], sourceRoot, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return int.MaxValue;
        }

        private int SourceOrderIndex(string sourceRoot)
        {
            return SourceOrderIndex(sourceRoot, GetSourceFolders());
        }

        private static bool IsMainArchiveCandidate(ConsolidationGroup group)
        {
            return group.SelectedFile != null &&
                   (group.Status == ConsolidationStatus.Unique ||
                    group.Status == ConsolidationStatus.DuplicateSameContent ||
                    group.Status == ConsolidationStatus.ConflictDifferentContent);
        }

        private AppArchiveStatistics CalculateArchiveStatistics()
        {
            int uniqueGroups = _groups.Count(g => g.Status == ConsolidationStatus.Unique && g.SelectedFile != null);
            int duplicateGroups = _groups.Count(g => g.Status == ConsolidationStatus.DuplicateSameContent && g.SelectedFile != null);
            int conflictGroups = _groups.Count(g => g.Status == ConsolidationStatus.ConflictDifferentContent);
            int errorGroups = _groups.Count(g => g.Status == ConsolidationStatus.Error);
            int toArchiveFiles = _groups.Count(IsMainArchiveCandidate);

            int duplicateFilesSkipped = _groups
                .Where(g => g.Status == ConsolidationStatus.DuplicateSameContent)
                .Sum(g => Math.Max(0, g.Files.Count - 1));

            int conflictVaultFiles = _groups
                .Where(g => g.Status == ConsolidationStatus.ConflictDifferentContent && g.SelectedFile != null)
                .Sum(g => Math.Max(0, g.Files.Count - 1));

            return new AppArchiveStatistics
            {
                UniqueGroups = uniqueGroups,
                DuplicateGroups = duplicateGroups,
                ConflictGroups = conflictGroups,
                ErrorGroups = errorGroups,
                ToArchiveFiles = toArchiveFiles,
                DuplicateFilesSkipped = duplicateFilesSkipped,
                ConflictVaultFiles = conflictVaultFiles
            };
        }

        private static AppCopyResult CopyOneFile(string relativePath, string sourceFile, string destinationFile, string? originalRelativePath = null, string copyRole = "Main Archive")
        {
            AppCopyResult result = new()
            {
                RelativePath = relativePath,
                OriginalRelativePath = originalRelativePath ?? relativePath,
                SourcePath = sourceFile,
                DestinationPath = destinationFile,
                CopyRole = copyRole,
                IsConflictVaultCopy = string.Equals(copyRole, "Conflict Vault", StringComparison.OrdinalIgnoreCase)
            };

            try
            {
                if (!File.Exists(sourceFile))
                {
                    result.Success = false;
                    result.Message = "Source file missing.";
                    return result;
                }

                string? destinationFolder = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrWhiteSpace(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);

                if (File.Exists(destinationFile))
                {
                    result.Success = false;
                    result.Message = "Target file already exists; FileForge does not overwrite automatically.";
                    return result;
                }

                File.Copy(sourceFile, destinationFile, overwrite: false);
                FileInfo info = new(destinationFile);

                result.Success = true;
                result.BytesCopied = info.Length;
                result.Message = string.Equals(copyRole, "Conflict Vault", StringComparison.OrdinalIgnoreCase)
                    ? "Older-dated conflicting version preserved in conflict vault."
                    : "Copied to main archive.";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

        private static int CreateEmptyDirectoryStructure(IEnumerable<string> sourceRoots, string targetRoot)
        {
            int created = 0;
            foreach (string sourceRoot in sourceRoots)
            {
                if (!Directory.Exists(sourceRoot))
                    continue;

                foreach (string sourceDirectory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
                {
                    string relativeDirectory = Path.GetRelativePath(sourceRoot, sourceDirectory);
                    if (string.IsNullOrWhiteSpace(relativeDirectory) || relativeDirectory == ".")
                        continue;

                    string targetDirectory = Path.Combine(targetRoot, relativeDirectory);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                        created++;
                    }
                }
            }

            return created;
        }

        private static string BuildConflictVaultDestination(string targetRoot, string originalRelativePath, SourceFileRecord sourceFile, IEnumerable<string> sourceRoots, int conflictIndex)
        {
            string sourceLabel = BuildSourceLabel(sourceFile.SourceRoot, sourceRoots);
            string fileName = Path.GetFileName(originalRelativePath);
            string directory = Path.GetDirectoryName(originalRelativePath) ?? string.Empty;
            string vaultFolder = Path.Combine(targetRoot, "_FileForge_Conflicts", directory, fileName, sourceLabel);
            string candidate = Path.Combine(vaultFolder, fileName);

            if (!File.Exists(candidate))
                return candidate;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            return Path.Combine(vaultFolder, $"{name}_{conflictIndex:00}{ext}");
        }

        private static string BuildSourceLabel(string sourceRoot, IEnumerable<string> sourceRoots)
        {
            int index = 1;
            foreach (string root in sourceRoots)
            {
                if (string.Equals(NormalizeDirectoryPath(root), NormalizeDirectoryPath(sourceRoot), StringComparison.OrdinalIgnoreCase))
                    return $"Source-{index:00}_{SafePathSegment(new DirectoryInfo(root).Name)}";
                index++;
            }

            return $"Source-XX_{SafePathSegment(new DirectoryInfo(sourceRoot).Name)}";
        }

        private static string BuildVaultRelativePath(string targetRoot, string vaultPath)
        {
            return Path.GetRelativePath(targetRoot, vaultPath);
        }

        private static string SafePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Source";

            char[] invalid = Path.GetInvalidFileNameChars();
            string cleaned = new(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            cleaned = cleaned.Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "Source" : cleaned;
        }

        private static void EnsureSelectedHashes(IEnumerable<ConsolidationGroup> expectedGroups)
        {
            foreach (SourceFileRecord file in expectedGroups.Select(g => g.SelectedFile).Where(f => f != null).Cast<SourceFileRecord>())
            {
                if (file.HashCalculated && !string.IsNullOrWhiteSpace(file.Sha256Hash))
                    continue;

                file.Sha256Hash = ComputeSha256(file.FullPath);
                file.HashCalculated = true;
            }
        }

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            byte[] hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private string BuildScanDetails(SourceFileRecord file)
        {
            return
                $"Relative Path : {file.RelativePath}{Environment.NewLine}" +
                $"File Name     : {file.FileName}{Environment.NewLine}" +
                $"Source Root   : {file.SourceRoot}{Environment.NewLine}" +
                $"Full Path     : {file.FullPath}{Environment.NewLine}" +
                $"Directory     : {file.DirectoryPath}{Environment.NewLine}" +
                $"Size          : {FormatBytes(file.SizeBytes)} ({file.SizeBytes:N0} bytes){Environment.NewLine}" +
                $"Created       : {file.CreatedTime:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                $"Modified      : {file.LastModifiedTime:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                "Hash Status   : Not calculated during Scan";
        }

        private string BuildAnalysisDetails(ConsolidationGroup group)
        {
            StringBuilder sb = new();
            sb.AppendLine($"Relative Path : {group.RelativePath}");
            sb.AppendLine($"Status        : {FormatStatus(group.Status)}");
            sb.AppendLine($"File Count    : {group.Files.Count:N0}");
            sb.AppendLine($"Decision      : {group.DecisionReason}");
            sb.AppendLine();

            if (group.SelectedFile != null)
            {
                SourceFileRecord selected = group.SelectedFile;
                sb.AppendLine(group.Status == ConsolidationStatus.ConflictDifferentContent ? "Main Archive Version" : "Selected File");
                sb.AppendLine("--------------------");
                sb.AppendLine($"Source Root   : {selected.SourceRoot}");
                sb.AppendLine($"Full Path     : {selected.FullPath}");
                sb.AppendLine($"Size          : {FormatBytes(selected.SizeBytes)} ({selected.SizeBytes:N0} bytes)");
                sb.AppendLine($"Modified      : {selected.LastModifiedTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Hash          : {(selected.HashCalculated ? selected.Sha256Hash : "Not required/calculated")}");
                sb.AppendLine();
            }

            if (group.Status == ConsolidationStatus.ConflictDifferentContent)
            {
                sb.AppendLine("Conflict Auto-Resolution");
                sb.AppendLine("------------------------");
                sb.AppendLine("Main archive version selected by latest modified date.");
                sb.AppendLine("Older-dated conflicting versions are preserved under _FileForge_Conflicts during Copy.");
                sb.AppendLine();
            }

            sb.AppendLine("All Source Candidates");
            sb.AppendLine("---------------------");

            foreach (SourceFileRecord file in group.Files.OrderBy(f => SourceOrderIndex(f.SourceRoot)).ThenByDescending(f => f.LastModifiedTime).ThenByDescending(f => f.SizeBytes))
            {
                bool isSelected = group.SelectedFile != null && string.Equals(group.SelectedFile.FullPath, file.FullPath, StringComparison.OrdinalIgnoreCase);
                sb.AppendLine(isSelected ? "[SELECTED]" : group.Status == ConsolidationStatus.ConflictDifferentContent ? "[CONFLICT VAULT VERSION]" : "[SKIPPED]");
                sb.AppendLine($"Source Root   : {file.SourceRoot}");
                sb.AppendLine($"Full Path     : {file.FullPath}");
                sb.AppendLine($"Size          : {FormatBytes(file.SizeBytes)} ({file.SizeBytes:N0} bytes)");
                sb.AppendLine($"Modified      : {file.LastModifiedTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Hash          : {(file.HashCalculated ? file.Sha256Hash : "Not required/calculated")}");
                if (!string.IsNullOrWhiteSpace(file.ErrorMessage))
                    sb.AppendLine($"Error         : {file.ErrorMessage}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string BuildCopyDetails(ConsolidationGroup group, AppCopyResult? copyResult)
        {
            StringBuilder sb = new();
            sb.AppendLine("Copy Result");
            sb.AppendLine("-----------");
            sb.AppendLine($"Relative Path : {group.RelativePath}");
            sb.AppendLine($"Decision      : {FormatStatus(group.Status)}");
            sb.AppendLine($"Reason        : {group.DecisionReason}");
            sb.AppendLine();

            if (copyResult == null)
            {
                sb.AppendLine("Copy Status   : Not copied / no copy result available.");
            }
            else
            {
                sb.AppendLine($"Copy Status   : {(copyResult.Skipped ? "Skipped" : copyResult.Success ? "Copied" : "Failed")}");
                sb.AppendLine($"Message       : {copyResult.Message}");
                sb.AppendLine($"Source        : {copyResult.SourcePath}");
                sb.AppendLine($"Target        : {copyResult.DestinationPath}");
                if (copyResult.BytesCopied > 0)
                    sb.AppendLine($"Bytes Copied  : {copyResult.BytesCopied:N0} ({FormatBytes(copyResult.BytesCopied)})");
            }

            if (group.Status == ConsolidationStatus.ConflictDifferentContent)
            {
                List<AppCopyResult> vaultCopies = _copyResultRecords
                    .Where(r => r.IsConflictVaultCopy && string.Equals(r.OriginalRelativePath, group.RelativePath, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                sb.AppendLine();
                sb.AppendLine("Conflict Vault Copies");
                sb.AppendLine("---------------------");
                foreach (AppCopyResult vaultCopy in vaultCopies)
                {
                    sb.AppendLine(vaultCopy.Success ? "[PRESERVED]" : "[FAILED]");
                    sb.AppendLine($"Source        : {vaultCopy.SourcePath}");
                    sb.AppendLine($"Vault Target  : {vaultCopy.DestinationPath}");
                    sb.AppendLine($"Message       : {vaultCopy.Message}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine("Analysis Details");
            sb.AppendLine("----------------");
            sb.Append(BuildAnalysisDetails(group));
            return sb.ToString();
        }

        private string BuildVerificationDetails(ConsolidationGroup group, CopyVerificationResult? verificationResult)
        {
            StringBuilder sb = new();
            sb.AppendLine("Verification Result");
            sb.AppendLine("-------------------");
            sb.AppendLine($"Relative Path : {group.RelativePath}");
            sb.AppendLine($"Decision      : {FormatStatus(group.Status)}");
            sb.AppendLine($"Reason        : {group.DecisionReason}");
            sb.AppendLine();

            if (verificationResult == null)
            {
                sb.AppendLine("Verify Status : Not verified / no verification result available.");
            }
            else
            {
                sb.AppendLine($"Verify Status : {(verificationResult.IsVerified ? "Verified" : "Failed")}");
                sb.AppendLine($"Failure Type  : {verificationResult.Status}");
                sb.AppendLine($"Message       : {verificationResult.Message}");
                sb.AppendLine($"Source        : {verificationResult.SourcePath}");
                sb.AppendLine($"Target        : {verificationResult.DestinationPath}");
                sb.AppendLine($"Expected Size : {verificationResult.ExpectedSizeBytes:N0} bytes ({FormatBytes(verificationResult.ExpectedSizeBytes)})");
                sb.AppendLine($"Actual Size   : {verificationResult.ActualSizeBytes:N0} bytes ({FormatBytes(verificationResult.ActualSizeBytes)})");
                sb.AppendLine();
                sb.AppendLine("Hash Check");
                sb.AppendLine("----------");
                sb.AppendLine($"Source Hash   : {BlankIfMissing(verificationResult.ExpectedHash)}");
                sb.AppendLine($"Target Hash   : {BlankIfMissing(verificationResult.ActualHash)}");
            }

            sb.AppendLine();
            sb.AppendLine("Analysis Details");
            sb.AppendLine("----------------");
            sb.Append(BuildAnalysisDetails(group));
            return sb.ToString();
        }

        private static string BuildOtherInfo(ConsolidationGroup group)
        {
            return group.Status switch
            {
                ConsolidationStatus.Unique => "Single source file",
                ConsolidationStatus.DuplicateSameContent => $"{Math.Max(0, group.Files.Count - 1):N0} duplicate file(s) skipped",
                ConsolidationStatus.ConflictDifferentContent => $"Auto-resolved; {Math.Max(0, group.Files.Count - 1):N0} older version(s) to conflict vault",
                ConsolidationStatus.Error => group.DecisionReason,
                _ => string.Empty
            };
        }

        private static string FormatStatus(ConsolidationStatus status)
        {
            return status switch
            {
                ConsolidationStatus.Unique => "Unique",
                ConsolidationStatus.DuplicateSameContent => "Duplicate (Same Content)",
                ConsolidationStatus.ConflictDifferentContent => "Conflict (Auto-Resolved)",
                ConsolidationStatus.Error => "Error",
                _ => "Unknown"
            };
        }

        private Color StatusColor(ConsolidationStatus status)
        {
            return status switch
            {
                ConsolidationStatus.Unique => _blue,
                ConsolidationStatus.DuplicateSameContent => _green,
                ConsolidationStatus.ConflictDifferentContent => Color.FromArgb(230, 105, 15),
                ConsolidationStatus.Error => Color.FromArgb(175, 45, 45),
                _ => _text
            };
        }

        private static string BlankIfMissing(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Not available" : value;
        }

        private static string NormalizeDirectoryPath(string path)
        {
            return Path.GetFullPath(path.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void OpenFile(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.##} {units[unitIndex]}";
        }

        private sealed class MetricCard : Control
        {
            private readonly string _title;
            private string _value;
            private readonly Color _accent;
            private readonly MetricIcon _icon;

            public MetricCard(string title, string value, Color accent, MetricIcon icon)
            {
                _title = title;
                _value = value;
                _accent = accent;
                _icon = icon;

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

                BackColor = Color.White;
                Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }

            public void SetValue(string value)
            {
                _value = value;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(BackColor);

                int iconX = 10;
                int iconY = Math.Max(7, (Height - 30) / 2);
                DrawMetricIcon(g, new Rectangle(iconX, iconY, 26, 26));

                using Brush titleBrush = new SolidBrush(Color.FromArgb(88, 103, 128));
                using Brush valueBrush = new SolidBrush(_accent);
                using Font titleFont = new("Segoe UI", 8.5F, FontStyle.Regular);
                using Font valueFont = new("Segoe UI", 14F, FontStyle.Bold);

                int textX = 48;
                g.DrawString(_title, titleFont, titleBrush, textX, 7);
                g.DrawString(_value, valueFont, valueBrush, textX, 24);
            }

            private void DrawMetricIcon(Graphics g, Rectangle r)
            {
                using Pen pen = new(_accent, 2.2F)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                };

                using Pen thin = new(_accent, 1.6F)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                };

                switch (_icon)
                {
                    case MetricIcon.File:
                        DrawDocument(g, r, pen, thin);
                        break;

                    case MetricIcon.Archive:
                        DrawDocument(g, r, pen, thin);
                        g.DrawLine(pen, r.Left + 7, r.Top + 14, r.Left + 11, r.Top + 18);
                        g.DrawLine(pen, r.Left + 11, r.Top + 18, r.Left + 18, r.Top + 9);
                        break;

                    case MetricIcon.Duplicate:
                        g.DrawRectangle(thin, r.Left + 4, r.Top + 8, 12, 13);
                        g.DrawRectangle(pen, r.Left + 8, r.Top + 4, 12, 13);
                        break;

                    case MetricIcon.Conflict:
                        Point[] octagon =
                        {
                            new(r.Left + 10, r.Top + 3),
                            new(r.Left + 17, r.Top + 3),
                            new(r.Left + 23, r.Top + 9),
                            new(r.Left + 23, r.Top + 17),
                            new(r.Left + 17, r.Top + 23),
                            new(r.Left + 10, r.Top + 23),
                            new(r.Left + 4, r.Top + 17),
                            new(r.Left + 4, r.Top + 9)
                        };
                        g.DrawPolygon(pen, octagon);
                        g.DrawLine(pen, r.Left + 14, r.Top + 8, r.Left + 14, r.Top + 15);
                        g.DrawEllipse(pen, r.Left + 13, r.Top + 18, 2, 2);
                        break;

                    case MetricIcon.Verify:
                        Point[] shield =
                        {
                            new(r.Left + 13, r.Top + 3),
                            new(r.Left + 21, r.Top + 7),
                            new(r.Left + 20, r.Top + 16),
                            new(r.Left + 13, r.Top + 23),
                            new(r.Left + 6, r.Top + 16),
                            new(r.Left + 5, r.Top + 7)
                        };
                        g.DrawPolygon(pen, shield);
                        g.DrawLine(pen, r.Left + 9, r.Top + 13, r.Left + 12, r.Top + 16);
                        g.DrawLine(pen, r.Left + 12, r.Top + 16, r.Left + 18, r.Top + 9);
                        break;
                }
            }

            private static void DrawDocument(Graphics g, Rectangle r, Pen pen, Pen thin)
            {
                Point[] doc =
                {
                    new(r.Left + 7, r.Top + 3),
                    new(r.Left + 16, r.Top + 3),
                    new(r.Left + 21, r.Top + 8),
                    new(r.Left + 21, r.Top + 23),
                    new(r.Left + 7, r.Top + 23)
                };

                g.DrawPolygon(pen, doc);
                g.DrawLine(thin, r.Left + 16, r.Top + 3, r.Left + 16, r.Top + 8);
                g.DrawLine(thin, r.Left + 16, r.Top + 8, r.Left + 21, r.Top + 8);
            }
        }

        private enum MetricIcon
        {
            File,
            Archive,
            Duplicate,
            Conflict,
            Verify
        }

        private enum ToolbarIcon
        {
            Scan,
            Analyze,
            Copy,
            Verify,
            Report
        }

        private enum UiWorkflowState
        {
            SetupRequired,
            ReadyToScan,
            ReadyToAnalyze,
            ReadyToCopy,
            ReadyToVerify,
            ReadyToReport
        }
    }


    internal sealed class AppArchiveStatistics
    {
        public int UniqueGroups { get; set; }
        public int DuplicateGroups { get; set; }
        public int ConflictGroups { get; set; }
        public int ErrorGroups { get; set; }
        public int ToArchiveFiles { get; set; }
        public int DuplicateFilesSkipped { get; set; }
        public int ConflictVaultFiles { get; set; }
    }

    internal enum AppResultsMode
    {
        Scan = 0,
        Analysis = 1,
        Copy = 2,
        Verify = 3
    }

    internal sealed class AppCopyResult
    {
        public string RelativePath { get; set; } = string.Empty;
        public string OriginalRelativePath { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; } = string.Empty;
        public long BytesCopied { get; set; }
        public string CopyRole { get; set; } = "Main Archive";
        public bool IsConflictVaultCopy { get; set; }
    }

    internal static class AppMultiFolderPicker
    {
        public static List<string> ShowDialog(IntPtr ownerHandle, string title)
        {
            List<string> folders = new();
            Type? dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7"));

            if (dialogType == null)
                return FallbackSingleFolder(ownerHandle, title);

            object? dialogObject = Activator.CreateInstance(dialogType);
            if (dialogObject == null)
                return FallbackSingleFolder(ownerHandle, title);

            IFileOpenDialog dialog = (IFileOpenDialog)dialogObject;

            try
            {
                dialog.GetOptions(out FOS options);
                options |= FOS.FOS_PICKFOLDERS;
                options |= FOS.FOS_ALLOWMULTISELECT;
                options |= FOS.FOS_FORCEFILESYSTEM;
                options |= FOS.FOS_PATHMUSTEXIST;

                dialog.SetOptions(options);
                dialog.SetTitle(title);
                dialog.SetOkButtonLabel("Add Selected Folders");

                int hr = dialog.Show(ownerHandle);

                if (hr == unchecked((int)0x800704C7))
                    return folders;

                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);

                dialog.GetResults(out IShellItemArray results);

                try
                {
                    results.GetCount(out uint count);

                    for (uint i = 0; i < count; i++)
                    {
                        results.GetItemAt(i, out IShellItem item);

                        try
                        {
                            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr pathPointer);

                            try
                            {
                                string? path = Marshal.PtrToStringUni(pathPointer);
                                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                                    folders.Add(path);
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(pathPointer);
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(item);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(results);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialogObject);
            }

            return folders;
        }

        private static List<string> FallbackSingleFolder(IntPtr ownerHandle, string title)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = title,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            return dialog.ShowDialog() == DialogResult.OK
                ? new List<string> { dialog.SelectedPath }
                : new List<string>();
        }

        [ComImport]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, uint fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IShellItemArray ppenum);
            void GetSelectedItems(out IShellItemArray ppsai);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);
            void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
            void GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);
            void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);
            void GetCount(out uint pdwNumItems);
            void GetItemAt(uint dwIndex, out IShellItem ppsi);
            void EnumItems(out IntPtr ppenumShellItems);
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000
        }
    }
}
