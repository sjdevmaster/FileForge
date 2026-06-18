using System;
using System.Drawing;
using System.Windows.Forms;

namespace FileForge.WinForms
{
    public class frmMainUI : Form
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

        public frmMainUI()
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
            SetWorkflowState(UiWorkflowState.SetupRequired);
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
            btnOptions.Click += (_, _) => MessageBox.Show("Options will be wired after UI approval.", "FileForge");
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

            btnScan.Click += (_, _) => NotWired("Scan");
            btnAnalyze.Click += (_, _) => NotWired("Analyze");
            btnCopy.Click += (_, _) => NotWired("Copy");
            btnVerify.Click += (_, _) => NotWired("Verify");
            btnReport.Click += (_, _) => NotWired("Report");

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
            btnAddSource.Click += (_, _) => NotWired("Add Source");
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
            btnRemoveSource.Click += (_, _) => NotWired("Remove Selected");
            pnlSource.Controls.Add(btnRemoveSource);

            btnClearSources = CreateSecondaryButton("Clear All");
            btnClearSources.Click += (_, _) => NotWired("Clear All");
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
            btnChangeTarget.Click += (_, _) => NotWired("Change Target");
            pnlTarget.Controls.Add(btnChangeTarget);

            btnOpenTarget = CreateSecondaryButton("Open Target");
            btnOpenTarget.Click += (_, _) => NotWired("Open Target");
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
            btnBrowseTarget.Click += (_, _) => NotWired("Browse Target");
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

            var filter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            filter.Items.AddRange(new object[] { "All Decisions", "Unique", "Duplicates", "Conflicts" });
            filter.SelectedIndex = 0;
            filter.Name = "cmbDecisionFilter";
            pnlResults.Controls.Add(filter);

            var search = new TextBox
            {
                Font = new Font("Segoe UI", 9F),
                Text = ""
            };
            search.Name = "txtSearch";
            pnlResults.Controls.Add(search);

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
                Text = "UI prototype loaded. Preserve Empty Directories is in Options; Open Target sits with Change Target.",
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
            Control filter = pnlResults.Controls["cmbDecisionFilter"];
            Control search = pnlResults.Controls["txtSearch"];

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

        private void SetWorkflowState(UiWorkflowState state)
        {
            ResetToolbarButtons();

            switch (state)
            {
                case UiWorkflowState.ReadyToScan:
                    HighlightToolbarButton(btnScan);
                    btnScan.Enabled = true;
                    break;

                case UiWorkflowState.ReadyToAnalyze:
                    MarkCompleted(btnScan);
                    HighlightToolbarButton(btnAnalyze);
                    btnAnalyze.Enabled = true;
                    break;

                case UiWorkflowState.ReadyToCopy:
                    MarkCompleted(btnScan);
                    MarkCompleted(btnAnalyze);
                    HighlightToolbarButton(btnCopy);
                    btnCopy.Enabled = true;
                    break;

                case UiWorkflowState.ReadyToVerify:
                    MarkCompleted(btnScan);
                    MarkCompleted(btnAnalyze);
                    MarkCompleted(btnCopy);
                    HighlightToolbarButton(btnVerify);
                    btnVerify.Enabled = true;
                    break;

                case UiWorkflowState.ReadyToReport:
                    MarkCompleted(btnScan);
                    MarkCompleted(btnAnalyze);
                    MarkCompleted(btnCopy);
                    MarkCompleted(btnVerify);
                    HighlightToolbarButton(btnReport);
                    btnReport.Enabled = true;
                    break;

                default:
                    break;
            }
        }

        private void ResetToolbarButtons()
        {
            foreach (Button button in new[] { btnScan, btnAnalyze, btnCopy, btnVerify, btnReport })
            {
                button.Enabled = false;
                button.BackColor = Color.White;
                button.ForeColor = _muted;
                button.FlatAppearance.BorderColor = _border;
                button.Text = button.Text.Replace("✓ ", string.Empty);
                RefreshToolbarIcon(button, _muted);
            }
        }

        private void HighlightToolbarButton(Button button)
        {
            button.Enabled = true;
            button.BackColor = _blue;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = _blue;
            RefreshToolbarIcon(button, Color.White);
        }

        private void MarkCompleted(Button button)
        {
            button.Enabled = true;
            if (!button.Text.StartsWith("✓ ", StringComparison.Ordinal))
            {
                button.Text = "✓ " + button.Text;
            }

            button.BackColor = Color.FromArgb(238, 248, 242);
            button.ForeColor = _green;
            button.FlatAppearance.BorderColor = Color.FromArgb(190, 224, 203);
            RefreshToolbarIcon(button, _green);
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

        private void NotWired(string action)
        {
            MessageBox.Show($"{action} is not wired in frmMainUI yet.", "FileForge UI Prototype");
        }

        private sealed class MetricCard : Control
        {
            private readonly string _title;
            private readonly string _value;
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
}
