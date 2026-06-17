using FileForge.Application.Services;
using FileForge.Domain.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FileForge.WinForms;

public partial class frmMain : Form
{
    private const int OuterMargin = 18;
    private const int PanelGap = 12;
    private const int HeaderHeight = 120;
    private const int SourceTargetHeight = 190;
    private const int DetailsHeight = 128;
    private const int StatusHeight = 42;

    private readonly FolderScanService _folderScanService = new();
    private readonly FileHashService _fileHashService = new();
    private readonly FileSelectionService _fileSelectionService = new();

    private readonly List<SourceFileRecord> _scannedFiles = new();
    private readonly List<ConsolidationGroup> _groups = new();
    private ResultsMode _resultsMode = ResultsMode.Scan;

    private Panel pnlHeader = null!;
    private Label lblTitle = null!;
    private Label lblTagline = null!;

    private readonly Label lblSources = new();
    private readonly Label lblFiles = new();
    private readonly Label lblUnique = new();
    private readonly Label lblDuplicates = new();
    private readonly Label lblConflicts = new();

    private Button btnScan = null!;
    private Button btnAnalyze = null!;
    private Button btnCopy = null!;
    private Button btnVerify = null!;
    private Button btnReport = null!;
    private Button btnOptions = null!;

    private Panel pnlSource = null!;
    private Label lblSourceTitle = null!;
    private Button btnAddSource = null!;
    private Button btnRemoveSource = null!;
    private Button btnClearSources = null!;
    private ListBox lstSourceFolders = null!;
    private Label lblSourceTip = null!;

    private Panel pnlTarget = null!;
    private Label lblTargetTitle = null!;
    private Label lblTargetFolder = null!;
    private TextBox txtTargetFolder = null!;
    private Button btnBrowseTarget = null!;
    private Button btnSelectTarget = null!;
    private Button btnOpenTarget = null!;
    private CheckBox chkPreserveEmptyDirectories = null!;
    private Label lblTargetMode = null!;

    private Panel pnlResults = null!;
    private Label lblResultsTitle = null!;
    private DataGridView dgvResults = null!;

    private Panel pnlDetails = null!;
    private Label lblDetailsTitle = null!;
    private TextBox txtDetails = null!;

    private Panel pnlStatus = null!;
    private Label lblStatusTitle = null!;
    private Label lblStatusMessage = null!;

    private readonly Color _background = Color.FromArgb(245, 247, 250);
    private readonly Color _panel = Color.White;
    private readonly Color _blue = Color.FromArgb(28, 87, 164);
    private readonly Color _darkBlue = Color.FromArgb(11, 47, 92);
    private readonly Color _green = Color.FromArgb(18, 128, 78);
    private readonly Color _purple = Color.FromArgb(96, 69, 170);
    private readonly Color _teal = Color.FromArgb(0, 125, 140);
    private readonly Color _darkButton = Color.FromArgb(68, 78, 92);
    private readonly Color _muted = Color.FromArgb(75, 88, 108);

    public frmMain()
    {
        InitializeComponent();
        BuildUi();
    }

    private void BuildUi()
    {
        SuspendLayout();

        Text = "FileForge - Professional File Consolidation";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        ClientSize = new Size(1280, 720);
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        BackColor = _background;

        Controls.Clear();

        BuildHeader();
        BuildSourcePanel();
        BuildTargetPanel();
        BuildResultsPanel();
        BuildDetailsPanel();
        BuildStatusPanel();

        Controls.Add(pnlHeader);
        Controls.Add(pnlSource);
        Controls.Add(pnlTarget);
        Controls.Add(pnlResults);
        Controls.Add(pnlDetails);
        Controls.Add(pnlStatus);

        ConfigureGridColumns();
        WireEvents();
        SetStatValues(0, 0, 0, 0, 0);
        SetStatus("Ready", "No operation in progress.");
        LayoutForm();

        Resize += (_, _) => LayoutForm();
        Shown += (_, _) => LayoutForm();

        ResumeLayout(true);
    }

    private void BuildHeader()
    {
        pnlHeader = new Panel
        {
            BackColor = _background,
            BorderStyle = BorderStyle.None
        };

        lblTitle = new Label
        {
            Text = "FileForge",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = _darkBlue,
            BackColor = Color.Transparent
        };

        lblTagline = new Label
        {
            Text = "Consolidate  •  Deduplicate  •  Verify  •  Archive",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
            ForeColor = _muted,
            BackColor = Color.Transparent
        };

        btnScan = CreateButton("Scan", _blue);
        btnAnalyze = CreateButton("Analyze", _green);
        btnCopy = CreateButton("Copy", _purple);
        btnVerify = CreateButton("Verify", _teal);
        btnReport = CreateButton("Report", _blue);
        btnOptions = CreateButton("Options", _darkButton);

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblTagline);
        pnlHeader.Controls.Add(CreateStatCard("Sources", lblSources));
        pnlHeader.Controls.Add(CreateStatCard("Files", lblFiles));
        pnlHeader.Controls.Add(CreateStatCard("Unique", lblUnique));
        pnlHeader.Controls.Add(CreateStatCard("Duplicates", lblDuplicates));
        pnlHeader.Controls.Add(CreateStatCard("Conflicts", lblConflicts));
        pnlHeader.Controls.Add(btnScan);
        pnlHeader.Controls.Add(btnAnalyze);
        pnlHeader.Controls.Add(btnCopy);
        pnlHeader.Controls.Add(btnVerify);
        pnlHeader.Controls.Add(btnReport);
        pnlHeader.Controls.Add(btnOptions);
    }

    private Panel CreateStatCard(string caption, Label valueLabel)
    {
        Panel card = new()
        {
            BackColor = _panel,
            BorderStyle = BorderStyle.FixedSingle
        };

        Label captionLabel = new()
        {
            Text = caption,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = _muted,
            BackColor = _panel
        };

        valueLabel.Text = "0";
        valueLabel.AutoSize = false;
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        valueLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        valueLabel.ForeColor = _darkBlue;
        valueLabel.BackColor = _panel;

        card.Controls.Add(captionLabel);
        card.Controls.Add(valueLabel);
        card.Tag = captionLabel;
        return card;
    }

    private void BuildSourcePanel()
    {
        pnlSource = CreatePanel();
        lblSourceTitle = CreateSectionTitle("SOURCE ROOT FOLDERS");
        btnAddSource = CreateButton("+ Add Source Root", _blue);
        btnRemoveSource = CreateButton("Remove Selected", _darkButton);
        btnClearSources = CreateButton("Clear All", _darkButton);

        lstSourceFolders = new ListBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            SelectionMode = SelectionMode.MultiExtended,
            HorizontalScrollbar = true,
            ScrollAlwaysVisible = true,
            IntegralHeight = false
        };

        lblSourceTip = new Label
        {
            Text = "Tip: use Ctrl/Shift in the picker to add multiple folders. Use Ctrl/Shift here to remove multiple entries.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = _muted,
            BackColor = _panel
        };

        pnlSource.Controls.Add(lblSourceTitle);
        pnlSource.Controls.Add(btnAddSource);
        pnlSource.Controls.Add(btnRemoveSource);
        pnlSource.Controls.Add(btnClearSources);
        pnlSource.Controls.Add(lstSourceFolders);
        pnlSource.Controls.Add(lblSourceTip);
    }

    private void BuildTargetPanel()
    {
        pnlTarget = CreatePanel();
        lblTargetTitle = CreateSectionTitle("TARGET MASTER FOLDER");

        lblTargetFolder = new Label
        {
            Text = "Target Folder:",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 45, 60),
            BackColor = _panel
        };

        txtTargetFolder = new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };

        btnBrowseTarget = CreateButton("...", _darkButton);
        btnSelectTarget = CreateButton("Select Target Folder", _green);
        btnOpenTarget = CreateButton("Open Target Folder", _darkButton);

        chkPreserveEmptyDirectories = new CheckBox
        {
            Text = "Preserve Empty Directories",
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 45, 60),
            BackColor = _panel
        };

        lblTargetMode = new Label
        {
            Text = "Unchecked = Backup Archive mode. Checked = Full Folder Reconstruction mode.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = _muted,
            BackColor = _panel
        };

        pnlTarget.Controls.Add(lblTargetTitle);
        pnlTarget.Controls.Add(lblTargetFolder);
        pnlTarget.Controls.Add(txtTargetFolder);
        pnlTarget.Controls.Add(btnBrowseTarget);
        pnlTarget.Controls.Add(btnSelectTarget);
        pnlTarget.Controls.Add(btnOpenTarget);
        pnlTarget.Controls.Add(chkPreserveEmptyDirectories);
        pnlTarget.Controls.Add(lblTargetMode);
    }

    private void BuildResultsPanel()
    {
        pnlResults = CreatePanel();
        lblResultsTitle = CreateSectionTitle("PREVIEW / RESULTS");

        dgvResults = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            RowTemplate = { Height = 24 },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 32,
            ScrollBars = ScrollBars.Both
        };

        dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 242, 252);
        dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = _darkBlue;
        dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvResults.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        dgvResults.DefaultCellStyle.SelectionBackColor = _blue;
        dgvResults.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvResults.SelectionChanged += DgvResults_SelectionChanged;

        pnlResults.Controls.Add(lblResultsTitle);
        pnlResults.Controls.Add(dgvResults);
    }

    private void BuildDetailsPanel()
    {
        pnlDetails = CreatePanel();
        lblDetailsTitle = CreateSectionTitle("DETAILS");

        txtDetails = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9F, FontStyle.Regular),
            BackColor = Color.White
        };

        pnlDetails.Controls.Add(lblDetailsTitle);
        pnlDetails.Controls.Add(txtDetails);
    }

    private void BuildStatusPanel()
    {
        pnlStatus = new Panel
        {
            BackColor = _panel,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusTitle = new Label
        {
            Text = "Ready",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = _green,
            BackColor = _panel
        };

        lblStatusMessage = new Label
        {
            Text = "No operation in progress.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(45, 58, 76),
            BackColor = _panel
        };

        pnlStatus.Controls.Add(lblStatusTitle);
        pnlStatus.Controls.Add(lblStatusMessage);
    }

    private Panel CreatePanel()
    {
        return new Panel
        {
            BackColor = _panel,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = _blue,
            BackColor = _panel
        };
    }

    private Button CreateButton(string text, Color backColor)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = false,
            Height = 30,
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            UseVisualStyleBackColor = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void ConfigureGridColumns()
    {
        dgvResults.Columns.Clear();
        dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RelativePath",
            HeaderText = "Relative Path",
            FillWeight = 240,
            MinimumWidth = 220
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            FillWeight = 95,
            MinimumWidth = 110
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SourceRoot",
            HeaderText = "Source Root",
            FillWeight = 180,
            MinimumWidth = 180
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FullPath",
            HeaderText = "Full Path",
            FillWeight = 260,
            MinimumWidth = 260
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Size",
            HeaderText = "Size",
            FillWeight = 70,
            MinimumWidth = 80,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LastModified",
            HeaderText = "Last Modified",
            FillWeight = 115,
            MinimumWidth = 145
        });
    }

    private void WireEvents()
    {
        btnAddSource.Click += BtnAddSource_Click;
        btnRemoveSource.Click += BtnRemoveSource_Click;
        btnClearSources.Click += BtnClearSources_Click;
        btnBrowseTarget.Click += BtnSelectTarget_Click;
        btnSelectTarget.Click += BtnSelectTarget_Click;
        btnOpenTarget.Click += BtnOpenTarget_Click;
        btnScan.Click += BtnScan_Click;

        btnAnalyze.Click += BtnAnalyze_Click;
        btnCopy.Click += (_, _) => SetStatus("Not connected", "Copy will be reconnected after Analyze is confirmed.");
        btnVerify.Click += (_, _) => SetStatus("Not connected", "Verify will be reconnected after Copy is confirmed.");
        btnReport.Click += (_, _) => SetStatus("Not connected", "Report engine will be connected later.");
        btnOptions.Click += (_, _) => SetStatus("Options", "Preserve Empty Directories is available in the Target panel.");
    }

    private void LayoutForm()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        int clientWidth = ClientSize.Width;
        int clientHeight = ClientSize.Height;
        int contentWidth = Math.Max(900, clientWidth - (OuterMargin * 2));

        pnlHeader.SetBounds(OuterMargin, 8, contentWidth, HeaderHeight);

        lblTitle.SetBounds(0, 4, 380, 45);
        lblTagline.SetBounds(3, 50, 430, 24);

        int statStartX = Math.Max(465, contentWidth - 690);
        int statY = 18;
        int statGap = 8;
        int statWidth = Math.Max(118, (contentWidth - statStartX - (statGap * 4)) / 5);
        int statHeight = 44;
        LayoutStatCard(0, statStartX, statY, statWidth, statHeight);
        LayoutStatCard(1, statStartX + ((statWidth + statGap) * 1), statY, statWidth, statHeight);
        LayoutStatCard(2, statStartX + ((statWidth + statGap) * 2), statY, statWidth, statHeight);
        LayoutStatCard(3, statStartX + ((statWidth + statGap) * 3), statY, statWidth, statHeight);
        LayoutStatCard(4, statStartX + ((statWidth + statGap) * 4), statY, statWidth, statHeight);

        int commandY = 80;
        int commandX = 0;
        SetButtonBounds(btnScan, ref commandX, commandY, 90);
        SetButtonBounds(btnAnalyze, ref commandX, commandY, 105);
        SetButtonBounds(btnCopy, ref commandX, commandY, 90);
        SetButtonBounds(btnVerify, ref commandX, commandY, 90);
        SetButtonBounds(btnReport, ref commandX, commandY, 90);
        SetButtonBounds(btnOptions, ref commandX, commandY, 95);

        int topY = pnlHeader.Bottom + 4;
        int panelWidth = (contentWidth - PanelGap) / 2;
        pnlSource.SetBounds(OuterMargin, topY, panelWidth, SourceTargetHeight);
        pnlTarget.SetBounds(OuterMargin + panelWidth + PanelGap, topY, contentWidth - panelWidth - PanelGap, SourceTargetHeight);

        LayoutSourcePanel();
        LayoutTargetPanel();

        int statusY = clientHeight - OuterMargin - StatusHeight;
        pnlStatus.SetBounds(OuterMargin, statusY, contentWidth, StatusHeight);
        lblStatusTitle.SetBounds(14, 8, 160, 24);
        lblStatusMessage.SetBounds(178, 8, Math.Max(300, pnlStatus.Width - 195), 24);

        int detailsY = statusY - PanelGap - DetailsHeight;
        pnlDetails.SetBounds(OuterMargin, detailsY, contentWidth, DetailsHeight);
        lblDetailsTitle.SetBounds(12, 8, 250, 24);
        txtDetails.SetBounds(12, 36, Math.Max(200, pnlDetails.Width - 24), Math.Max(50, pnlDetails.Height - 48));

        int resultsY = pnlSource.Bottom + PanelGap;
        int resultsHeight = Math.Max(120, detailsY - PanelGap - resultsY);
        pnlResults.SetBounds(OuterMargin, resultsY, contentWidth, resultsHeight);
        lblResultsTitle.SetBounds(12, 8, 250, 24);
        dgvResults.SetBounds(12, 36, Math.Max(200, pnlResults.Width - 24), Math.Max(80, pnlResults.Height - 48));
    }

    private void LayoutStatCard(int index, int x, int y, int width, int height)
    {
        if (index < 0 || index >= 5)
            return;

        Control card = pnlHeader.Controls.OfType<Panel>().ElementAt(index);
        card.SetBounds(x, y, width, height);

        Label? caption = card.Tag as Label;
        Label value = index switch
        {
            0 => lblSources,
            1 => lblFiles,
            2 => lblUnique,
            3 => lblDuplicates,
            _ => lblConflicts
        };

        caption?.SetBounds(10, 9, Math.Max(30, width - 62), 24);
        value.SetBounds(width - 52, 4, 42, height - 8);
    }

    private static void SetButtonBounds(Button button, ref int x, int y, int width)
    {
        button.SetBounds(x, y, width, 30);
        x += width + 8;
    }

    private void LayoutSourcePanel()
    {
        int w = pnlSource.Width;
        lblSourceTitle.SetBounds(12, 8, 250, 24);
        btnAddSource.SetBounds(12, 38, 150, 30);
        btnRemoveSource.SetBounds(170, 38, 145, 30);
        btnClearSources.SetBounds(323, 38, 90, 30);
        lstSourceFolders.SetBounds(12, 78, Math.Max(200, w - 24), 82);
        lblSourceTip.SetBounds(12, 162, Math.Max(200, w - 24), 22);
    }

    private void LayoutTargetPanel()
    {
        int w = pnlTarget.Width;
        lblTargetTitle.SetBounds(12, 8, 250, 24);
        lblTargetFolder.SetBounds(12, 42, 100, 24);
        txtTargetFolder.SetBounds(112, 40, Math.Max(160, w - 184), 26);
        btnBrowseTarget.SetBounds(w - 62, 38, 50, 30);
        btnSelectTarget.SetBounds(112, 78, 165, 30);
        btnOpenTarget.SetBounds(285, 78, 150, 30);
        chkPreserveEmptyDirectories.SetBounds(112, 120, 230, 24);
        lblTargetMode.SetBounds(112, 148, Math.Max(200, w - 124), 24);
    }

    private void BtnAddSource_Click(object? sender, EventArgs e)
    {
        List<string> selectedFolders = MultiFolderPicker.ShowDialog(
            Handle,
            "Select one or more source root folders. Use Ctrl or Shift to select multiple folders.");

        if (selectedFolders.Count == 0)
            return;

        int added = 0;

        foreach (string folder in selectedFolders)
        {
            if (!lstSourceFolders.Items.Contains(folder))
            {
                lstSourceFolders.Items.Add(folder);
                added++;
            }
        }

        SetStatValues(lstSourceFolders.Items.Count, _scannedFiles.Count, 0, 0, 0);
        SetStatus("Source folders updated", $"Selected: {lstSourceFolders.Items.Count:N0}. Added: {added:N0}.");
    }

    private void BtnRemoveSource_Click(object? sender, EventArgs e)
    {
        if (lstSourceFolders.SelectedItems.Count == 0)
        {
            SetStatus("No source selected", "Select one or more source folders to remove.");
            return;
        }

        while (lstSourceFolders.SelectedItems.Count > 0)
            lstSourceFolders.Items.Remove(lstSourceFolders.SelectedItems[0]);

        ClearScanResultsOnly();
        SetStatValues(lstSourceFolders.Items.Count, 0, 0, 0, 0);
        SetStatus("Source folders updated", $"Selected: {lstSourceFolders.Items.Count:N0}.");
    }

    private void BtnClearSources_Click(object? sender, EventArgs e)
    {
        lstSourceFolders.Items.Clear();
        ClearScanResultsOnly();
        SetStatValues(0, 0, 0, 0, 0);
        SetStatus("Ready", "Source folders cleared.");
    }

    private void BtnSelectTarget_Click(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select the target master folder.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        txtTargetFolder.Text = dialog.SelectedPath;
        SetStatus("Target selected", dialog.SelectedPath);
    }

    private void BtnOpenTarget_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTargetFolder.Text) || !Directory.Exists(txtTargetFolder.Text))
        {
            SetStatus("Target unavailable", "Select a valid target folder first.", true);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = txtTargetFolder.Text,
            UseShellExecute = true
        });
    }

    private void BtnScan_Click(object? sender, EventArgs e)
    {
        if (lstSourceFolders.Items.Count == 0)
        {
            MessageBox.Show("Please add at least one source root folder.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            SetStatus("Scanning", "Scanning source folders. Please wait...");
            System.Windows.Forms.Application.DoEvents();

            ClearScanResultsOnly();
            _resultsMode = ResultsMode.Scan;

            List<string> sourceFolders = lstSourceFolders.Items.Cast<string>().ToList();
            List<SourceFileRecord> scanned = _folderScanService.ScanFolders(sourceFolders);

            _scannedFiles.Clear();
            _scannedFiles.AddRange(scanned);

            dgvResults.Rows.Clear();
            foreach (SourceFileRecord file in _scannedFiles)
            {
                dgvResults.Rows.Add(
                    file.RelativePath,
                    "Scanned",
                    file.SourceRoot,
                    file.FullPath,
                    FormatBytes(file.SizeBytes),
                    file.LastModifiedTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            SetStatValues(lstSourceFolders.Items.Count, _scannedFiles.Count, 0, 0, 0);
            SetStatus("Scan complete", $"Files found: {_scannedFiles.Count:N0}.");
        }
        catch (Exception ex)
        {
            SetStatus("Scan failed", ex.Message, true);
            MessageBox.Show(ex.Message, "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private async void BtnAnalyze_Click(object? sender, EventArgs e)
    {
        if (_scannedFiles.Count == 0)
        {
            MessageBox.Show("Please run Scan before Analyze.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetStatus("Analyze blocked", "Run Scan first.", true);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            SetCommandButtonsEnabled(false);
            SetStatus("Analyzing", "Preparing duplicate/conflict analysis. Please wait...");

            Progress<FileHashProgress> progress = new(hashProgress =>
            {
                if (hashProgress.Total <= 0)
                {
                    SetStatus("Analyzing", "No same-size duplicate-path files require hashing.");
                    return;
                }

                SetStatus(
                    "Analyzing",
                    $"Hashing required files {hashProgress.Completed:N0}/{hashProgress.Total:N0}: {hashProgress.CurrentFile}");
            });

            int hashedCount = await _fileHashService.CalculateRequiredHashesAsync(_scannedFiles, progress);
            List<string> sourceOrder = lstSourceFolders.Items.Cast<string>().ToList();

            _groups.Clear();
            _groups.AddRange(_fileSelectionService.BuildGroups(_scannedFiles, sourceOrder));

            _resultsMode = ResultsMode.Analysis;
            dgvResults.Rows.Clear();
            dgvResults.SuspendLayout();

            foreach (ConsolidationGroup group in _groups)
            {
                SourceFileRecord? selected = group.SelectedFile;

                dgvResults.Rows.Add(
                    group.RelativePath,
                    FormatStatus(group.Status),
                    selected?.SourceRoot ?? string.Empty,
                    selected?.FullPath ?? "Manual review required",
                    selected == null ? string.Empty : FormatBytes(selected.SizeBytes),
                    selected == null ? string.Empty : selected.LastModifiedTime.ToString("yyyy-MM-dd HH:mm:ss"));

                int rowIndex = dgvResults.Rows.Count - 1;
                dgvResults.Rows[rowIndex].DefaultCellStyle.ForeColor = StatusColor(group.Status);
            }

            dgvResults.ResumeLayout();

            int uniqueCount = _groups.Count(g => g.Status == ConsolidationStatus.Unique);
            int duplicateCount = _groups.Count(g => g.Status == ConsolidationStatus.DuplicateSameContent);
            int conflictCount = _groups.Count(g => g.Status == ConsolidationStatus.ConflictDifferentContent || g.Status == ConsolidationStatus.Error);

            SetStatValues(lstSourceFolders.Items.Count, _scannedFiles.Count, uniqueCount, duplicateCount, conflictCount);
            SetStatus("Analyze complete", $"Groups: {_groups.Count:N0}. Unique: {uniqueCount:N0}. Duplicates: {duplicateCount:N0}. Conflicts/Errors: {conflictCount:N0}. Hashed: {hashedCount:N0}.");

            if (dgvResults.Rows.Count > 0)
                dgvResults.Rows[0].Selected = true;
        }
        catch (Exception ex)
        {
            SetStatus("Analyze failed", ex.Message, true);
            MessageBox.Show(ex.Message, "Analyze Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetCommandButtonsEnabled(true);
            Cursor = Cursors.Default;
        }
    }

    private void SetCommandButtonsEnabled(bool enabled)
    {
        btnScan.Enabled = enabled;
        btnAnalyze.Enabled = enabled;
        btnCopy.Enabled = enabled;
        btnVerify.Enabled = enabled;
        btnReport.Enabled = enabled;
        btnOptions.Enabled = enabled;
        btnAddSource.Enabled = enabled;
        btnRemoveSource.Enabled = enabled;
        btnClearSources.Enabled = enabled;
        btnBrowseTarget.Enabled = enabled;
        btnSelectTarget.Enabled = enabled;
        btnOpenTarget.Enabled = enabled;
    }

    private void DgvResults_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvResults.SelectedRows.Count == 0)
            return;

        if (_resultsMode == ResultsMode.Analysis)
        {
            string relativePath = dgvResults.SelectedRows[0].Cells["RelativePath"].Value?.ToString() ?? string.Empty;
            ConsolidationGroup? group = _groups.FirstOrDefault(g =>
                string.Equals(g.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

            txtDetails.Text = group == null
                ? "Select an analyzed decision to view details."
                : BuildAnalysisDetails(group);

            return;
        }

        string fullPath = dgvResults.SelectedRows[0].Cells["FullPath"].Value?.ToString() ?? string.Empty;
        SourceFileRecord? file = _scannedFiles.FirstOrDefault(f =>
            string.Equals(f.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));

        txtDetails.Text = file == null
            ? "Select a scanned file to view details."
            : BuildScanDetails(file);
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
            $"Hash Status   : Not calculated during Scan";
    }

    private string BuildAnalysisDetails(ConsolidationGroup group)
    {
        StringBuilder sb = new();

        sb.AppendLine($"Relative Path : {group.RelativePath}");
        sb.AppendLine($"Status        : {FormatStatus(group.Status)}");
        sb.AppendLine($"File Count    : {group.FileCount:N0}");
        sb.AppendLine($"Decision      : {group.DecisionReason}");
        sb.AppendLine();

        if (group.SelectedFile != null)
        {
            SourceFileRecord selected = group.SelectedFile;
            sb.AppendLine("Selected File");
            sb.AppendLine("-------------");
            sb.AppendLine($"Source Root   : {selected.SourceRoot}");
            sb.AppendLine($"Full Path     : {selected.FullPath}");
            sb.AppendLine($"Size          : {FormatBytes(selected.SizeBytes)} ({selected.SizeBytes:N0} bytes)");
            sb.AppendLine($"Modified      : {selected.LastModifiedTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Hash          : {(selected.HashCalculated ? selected.Sha256Hash : "Not required/calculated")}");
            sb.AppendLine();
        }

        sb.AppendLine("All Source Candidates");
        sb.AppendLine("---------------------");

        foreach (SourceFileRecord file in group.Files
                     .OrderBy(f => SourceOrderIndex(f.SourceRoot))
                     .ThenByDescending(f => f.LastModifiedTime)
                     .ThenByDescending(f => f.SizeBytes))
        {
            bool isSelected = group.SelectedFile != null &&
                              string.Equals(group.SelectedFile.FullPath, file.FullPath, StringComparison.OrdinalIgnoreCase);

            sb.AppendLine(isSelected ? "[SELECTED]" : "[SKIPPED]");
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

    private void ClearScanResultsOnly()
    {
        _scannedFiles.Clear();
        _groups.Clear();
        _resultsMode = ResultsMode.Scan;
        dgvResults.Rows.Clear();
        txtDetails.Clear();
    }

    private void SetStatValues(int sources, int files, int unique, int duplicates, int conflicts)
    {
        lblSources.Text = sources.ToString("N0");
        lblFiles.Text = files.ToString("N0");
        lblUnique.Text = unique.ToString("N0");
        lblDuplicates.Text = duplicates.ToString("N0");
        lblConflicts.Text = conflicts.ToString("N0");
    }

    private int SourceOrderIndex(string sourceRoot)
    {
        for (int i = 0; i < lstSourceFolders.Items.Count; i++)
        {
            string item = lstSourceFolders.Items[i]?.ToString() ?? string.Empty;
            if (string.Equals(item, sourceRoot, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return int.MaxValue;
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

    private Color StatusColor(ConsolidationStatus status)
    {
        return status switch
        {
            ConsolidationStatus.Unique => Color.FromArgb(0, 95, 55),
            ConsolidationStatus.DuplicateSameContent => Color.FromArgb(125, 82, 0),
            ConsolidationStatus.ConflictDifferentContent => Color.FromArgb(175, 45, 45),
            ConsolidationStatus.Error => Color.FromArgb(175, 45, 45),
            _ => Color.FromArgb(35, 45, 60)
        };
    }

    private void SetStatus(string title, string message, bool isError = false)
    {
        lblStatusTitle.Text = title;
        lblStatusTitle.ForeColor = isError ? Color.FromArgb(175, 45, 45) : _green;
        lblStatusMessage.Text = message;
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
}

internal enum ResultsMode
{
    Scan = 0,
    Analysis = 1
}

internal static class MultiFolderPicker
{
    public static List<string> ShowDialog(IntPtr ownerHandle, string title)
    {
        List<string> folders = new();
        Type? dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7"));

        if (dialogType == null)
            return folders;

        object? dialogObject = Activator.CreateInstance(dialogType);
        if (dialogObject == null)
            return folders;

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
