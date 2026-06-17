using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FileForge.WinForms;

public partial class frmMain : Form
{
    private Panel pnlHeader = null!;
    private Label lblTitle = null!;
    private Label lblTagline = null!;
    private Panel pnlWorkspace = null!;

    private Panel pnlSource = null!;
    private Panel pnlTarget = null!;
    private Panel pnlResults = null!;
    private Label lblResultsTitle = null!;
    private DataGridView dgvResults = null!;

    private Panel pnlDetails = null!;
    private Label lblDetailsTitle = null!;
    private TextBox txtDetails = null!;

    private Panel pnlStatus = null!;
    private Label lblStatusTitle = null!;
    private Label lblStatusMessage = null!;

    private Label lblSourceTitle = null!;
    private Button btnAddSource = null!;
    private Button btnRemoveSource = null!;
    private Button btnClearSources = null!;
    private ListBox lstSourceFolders = null!;
    private Label lblSourceTip = null!;

    private Label lblTargetTitle = null!;
    private Label lblTargetFolder = null!;
    private TextBox txtTargetFolder = null!;
    private Button btnBrowseTarget = null!;
    private Button btnSelectTarget = null!;
    private Button btnOpenTarget = null!;
    private CheckBox chkPreserveEmptyDirectories = null!;
    private Label lblTargetMode = null!;

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
        MinimumSize = new Size(1100, 650);
        ClientSize = new Size(1280, 720);
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        BackColor = Color.FromArgb(245, 247, 250);

        Controls.Clear();

        BuildHeader();
        BuildWorkspace();

        Controls.Add(pnlHeader);
        Controls.Add(pnlWorkspace);

        SetStatValues(0, 0, 0, 0, 0);
        LayoutForm();

        WireEvents();

        Resize += (_, _) => LayoutForm();
        Shown += (_, _) => LayoutForm();

        ResumeLayout(true);
    }

    private void BuildHeader()
    {
        pnlHeader = new Panel
        {
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = Padding.Empty
        };

        lblTitle = new Label
        {
            Text = "FileForge",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = Color.FromArgb(13, 48, 92),
            BackColor = Color.Transparent
        };

        lblTagline = new Label
        {
            Text = "Consolidate  •  Deduplicate  •  Verify  •  Archive",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(55, 70, 92),
            BackColor = Color.Transparent
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblTagline);

        pnlHeader.Controls.Add(CreateStatCard("Sources", lblSources));
        pnlHeader.Controls.Add(CreateStatCard("Files", lblFiles));
        pnlHeader.Controls.Add(CreateStatCard("Unique", lblUnique));
        pnlHeader.Controls.Add(CreateStatCard("Duplicates", lblDuplicates));
        pnlHeader.Controls.Add(CreateStatCard("Conflicts", lblConflicts));

        btnScan = CreatePrimaryButton("Scan");
        btnAnalyze = CreateGreenButton("Analyze");
        btnCopy = CreatePurpleButton("Copy");
        btnVerify = CreateTealButton("Verify");
        btnReport = CreatePrimaryButton("Report");
        btnOptions = CreateDarkButton("Options");

        pnlHeader.Controls.Add(btnScan);
        pnlHeader.Controls.Add(btnAnalyze);
        pnlHeader.Controls.Add(btnCopy);
        pnlHeader.Controls.Add(btnVerify);
        pnlHeader.Controls.Add(btnReport);
        pnlHeader.Controls.Add(btnOptions);
    }

    private void BuildWorkspace()
    {
        pnlWorkspace = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = Padding.Empty
        };

        BuildSourcePanel();
        BuildTargetPanel();
        BuildResultsPanel();
        BuildDetailsPanel();
        BuildStatusPanel();

        pnlWorkspace.Controls.Add(pnlSource);
        pnlWorkspace.Controls.Add(pnlTarget);
        pnlWorkspace.Controls.Add(pnlResults);
        pnlWorkspace.Controls.Add(pnlDetails);
        pnlWorkspace.Controls.Add(pnlStatus);
    }

    private void BuildSourcePanel()
    {
        pnlSource = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblSourceTitle = CreateSectionTitle("SOURCE ROOT FOLDERS");

        btnAddSource = CreatePrimaryButton("+ Add Source Root");
        btnRemoveSource = CreateDarkButton("Remove Selected");
        btnClearSources = CreateDarkButton("Clear All");

        lstSourceFolders = new ListBox
        {
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            SelectionMode = SelectionMode.MultiExtended,
            BorderStyle = BorderStyle.FixedSingle,
            HorizontalScrollbar = true,
            ScrollAlwaysVisible = true,
            IntegralHeight = false
        };

        lblSourceTip = new Label
        {
            Text = "Tip: add source roots one by one. Use Ctrl/Shift in this list to remove multiple entries.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(70, 82, 105),
            BackColor = Color.White
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
        pnlTarget = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblTargetTitle = CreateSectionTitle("TARGET MASTER FOLDER");

        lblTargetFolder = new Label
        {
            Text = "Target Folder:",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 35, 50),
            BackColor = Color.White
        };

        txtTargetFolder = new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };

        btnBrowseTarget = CreateDarkButton("...");
        btnSelectTarget = CreateGreenButton("Select Target Folder");
        btnOpenTarget = CreateDarkButton("Open Target Folder");

        chkPreserveEmptyDirectories = new CheckBox
        {
            Text = "Preserve Empty Directories",
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 35, 50),
            BackColor = Color.White
        };

        lblTargetMode = new Label
        {
            Text = "Unchecked = Backup Archive mode. Checked = Full Folder Reconstruction mode.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(70, 82, 105),
            BackColor = Color.White
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
        pnlResults = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblResultsTitle = CreateSectionTitle("PREVIEW / RESULTS");

        dgvResults = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeight = 30,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            EnableHeadersVisualStyles = false,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            RowTemplate = { Height = 26 },
            ScrollBars = ScrollBars.Both,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };

        dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 241, 248);
        dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 45, 85);
        dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgvResults.DefaultCellStyle.BackColor = Color.White;
        dgvResults.DefaultCellStyle.ForeColor = Color.FromArgb(25, 35, 50);
        dgvResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(43, 96, 177);
        dgvResults.DefaultCellStyle.SelectionForeColor = Color.White;

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RelativePath",
            HeaderText = "Relative Path",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 34,
            MinimumWidth = 320
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 150
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SelectedFrom",
            HeaderText = "Selected From",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 30,
            MinimumWidth = 280
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Size",
            HeaderText = "Size",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 90
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LastModified",
            HeaderText = "Last Modified",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = 145
        });

        dgvResults.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Hash",
            HeaderText = "Hash",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 22,
            MinimumWidth = 180
        });

        pnlResults.Controls.Add(lblResultsTitle);
        pnlResults.Controls.Add(dgvResults);
    }

    private void BuildDetailsPanel()
    {
        pnlDetails = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblDetailsTitle = CreateSectionTitle("DETAILS");

        txtDetails = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(25, 35, 50),
            Font = new Font("Consolas", 9F, FontStyle.Regular),
            Text = "Decision and verification details will appear here after functionality is reconnected."
        };

        pnlDetails.Controls.Add(lblDetailsTitle);
        pnlDetails.Controls.Add(txtDetails);
    }

    private void BuildStatusPanel()
    {
        pnlStatus = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusTitle = new Label
        {
            Text = "Ready",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 110, 55),
            BackColor = Color.White
        };

        lblStatusMessage = new Label
        {
            Text = "No operation in progress.",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(55, 70, 92),
            BackColor = Color.White
        };

        pnlStatus.Controls.Add(lblStatusTitle);
        pnlStatus.Controls.Add(lblStatusMessage);
    }

    private Panel CreateStatCard(string caption, Label valueLabel)
    {
        var card = new Panel
        {
            Width = 150,
            Height = 44,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Tag = caption
        };

        var captionLabel = new Label
        {
            Text = caption,
            AutoSize = false,
            Bounds = new Rectangle(12, 0, 88, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(70, 82, 105),
            BackColor = Color.White
        };

        valueLabel.Text = "0";
        valueLabel.AutoSize = false;
        valueLabel.Bounds = new Rectangle(94, 0, 44, 42);
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        valueLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        valueLabel.ForeColor = Color.FromArgb(13, 48, 92);
        valueLabel.BackColor = Color.White;

        card.Controls.Add(captionLabel);
        card.Controls.Add(valueLabel);

        return card;
    }

    private static Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 75, 160),
            BackColor = Color.White
        };
    }

    private static Button CreatePrimaryButton(string text)
    {
        return CreateButton(text, Color.FromArgb(43, 96, 177));
    }

    private static Button CreateGreenButton(string text)
    {
        return CreateButton(text, Color.FromArgb(0, 132, 76));
    }

    private static Button CreatePurpleButton(string text)
    {
        return CreateButton(text, Color.FromArgb(104, 72, 160));
    }

    private static Button CreateTealButton(string text)
    {
        return CreateButton(text, Color.FromArgb(30, 139, 156));
    }

    private static Button CreateDarkButton(string text)
    {
        return CreateButton(text, Color.FromArgb(65, 77, 94));
    }

    private static Button CreateButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
    }

    private void LayoutForm()
    {
        if (pnlHeader == null || pnlWorkspace == null)
            return;

        const int headerHeight = 150;

        pnlHeader.SetBounds(0, 0, ClientSize.Width, headerHeight);
        pnlWorkspace.SetBounds(0, headerHeight, ClientSize.Width, Math.Max(100, ClientSize.Height - headerHeight));

        LayoutHeaderControls();
        LayoutWorkspaceControls();
    }

    private void LayoutHeaderControls()
    {
        if (pnlHeader == null || lblTitle == null || lblTagline == null)
            return;

        const int left = 30;
        const int top = 26;
        const int brandWidth = 360;

        lblTitle.SetBounds(left, top, brandWidth, 44);
        lblTagline.SetBounds(left, top + 48, brandWidth, 28);

        int cardWidth = 150;
        int cardHeight = 44;
        int gap = 10;
        int cardCount = 5;
        int totalCardsWidth = (cardWidth * cardCount) + (gap * (cardCount - 1));
        int startX = Math.Max(left + brandWidth + 40, pnlHeader.ClientSize.Width - totalCardsWidth - 30);
        int cardTop = 42;

        int index = 0;
        foreach (Control control in pnlHeader.Controls)
        {
            if (control is Panel panel && panel.Tag is string)
            {
                panel.SetBounds(startX + (index * (cardWidth + gap)), cardTop, cardWidth, cardHeight);
                index++;
            }
        }

        int buttonWidth = 120;
        int buttonHeight = 34;
        int buttonGap = 10;
        int buttonCount = 6;
        int totalButtonsWidth = (buttonWidth * buttonCount) + (buttonGap * (buttonCount - 1));
        int buttonStartX = Math.Max(left + brandWidth + 40, pnlHeader.ClientSize.Width - totalButtonsWidth - 30);
        int buttonTop = 104;

        btnScan.SetBounds(buttonStartX, buttonTop, buttonWidth, buttonHeight);
        btnAnalyze.SetBounds(buttonStartX + ((buttonWidth + buttonGap) * 1), buttonTop, buttonWidth, buttonHeight);
        btnCopy.SetBounds(buttonStartX + ((buttonWidth + buttonGap) * 2), buttonTop, buttonWidth, buttonHeight);
        btnVerify.SetBounds(buttonStartX + ((buttonWidth + buttonGap) * 3), buttonTop, buttonWidth, buttonHeight);
        btnReport.SetBounds(buttonStartX + ((buttonWidth + buttonGap) * 4), buttonTop, buttonWidth, buttonHeight);
        btnOptions.SetBounds(buttonStartX + ((buttonWidth + buttonGap) * 5), buttonTop, buttonWidth, buttonHeight);
    }

    private void LayoutWorkspaceControls()
    {
        if (pnlWorkspace == null || pnlSource == null || pnlTarget == null || pnlResults == null || pnlDetails == null || pnlStatus == null)
            return;

        const int margin = 28;
        const int gap = 18;
        const int panelHeight = 205;

        int availableWidth = Math.Max(800, pnlWorkspace.ClientSize.Width - (margin * 2) - gap);
        int panelWidth = availableWidth / 2;
        int top = 24;

        pnlSource.SetBounds(margin, top, panelWidth, panelHeight);
        pnlTarget.SetBounds(margin + panelWidth + gap, top, panelWidth, panelHeight);

        LayoutSourcePanel();
        LayoutTargetPanel();

        int resultsTop = top + panelHeight + 22;
        int detailsHeight = 115;
        int statusHeight = 48;
        int detailsGap = 14;
        int statusGap = 10;
        int availableHeight = pnlWorkspace.ClientSize.Height - resultsTop - detailsGap - detailsHeight - statusGap - statusHeight - margin;
        int resultsHeight = Math.Max(120, availableHeight);

        pnlResults.SetBounds(margin, resultsTop, pnlWorkspace.ClientSize.Width - (margin * 2), resultsHeight);
        LayoutResultsPanel();

        int detailsTop = pnlResults.Bottom + detailsGap;
        pnlDetails.SetBounds(margin, detailsTop, pnlWorkspace.ClientSize.Width - (margin * 2), detailsHeight);
        LayoutDetailsPanel();

        int statusTop = pnlDetails.Bottom + statusGap;
        pnlStatus.SetBounds(margin, statusTop, pnlWorkspace.ClientSize.Width - (margin * 2), statusHeight);
        LayoutStatusPanel();
    }

    private void LayoutSourcePanel()
    {
        int w = pnlSource.ClientSize.Width;

        lblSourceTitle.SetBounds(16, 10, w - 32, 22);
        btnAddSource.SetBounds(16, 42, 160, 34);
        btnRemoveSource.SetBounds(186, 42, 150, 34);
        btnClearSources.SetBounds(346, 42, 130, 34);
        lstSourceFolders.SetBounds(16, 86, w - 32, 96);
        lblSourceTip.SetBounds(16, 186, w - 32, 22);
    }

    private void LayoutTargetPanel()
    {
        int w = pnlTarget.ClientSize.Width;

        lblTargetTitle.SetBounds(16, 10, w - 32, 22);
        lblTargetFolder.SetBounds(24, 45, 110, 24);
        txtTargetFolder.SetBounds(138, 45, Math.Max(120, w - 220), 24);
        btnBrowseTarget.SetBounds(w - 70, 42, 46, 31);

        btnSelectTarget.SetBounds(138, 84, 180, 34);
        btnOpenTarget.SetBounds(328, 84, 165, 34);

        chkPreserveEmptyDirectories.SetBounds(138, 126, 250, 22);
        lblTargetMode.SetBounds(392, 126, Math.Max(100, w - 410), 22);
    }

    private void LayoutResultsPanel()
    {
        int w = pnlResults.ClientSize.Width;
        int h = pnlResults.ClientSize.Height;

        lblResultsTitle.SetBounds(16, 8, w - 32, 22);
        dgvResults.SetBounds(1, 36, Math.Max(100, w - 2), Math.Max(100, h - 37));
    }

    private void LayoutDetailsPanel()
    {
        int w = pnlDetails.ClientSize.Width;
        int h = pnlDetails.ClientSize.Height;

        lblDetailsTitle.SetBounds(16, 8, w - 32, 22);
        txtDetails.SetBounds(16, 36, Math.Max(100, w - 32), Math.Max(60, h - 44));
    }

    private void LayoutStatusPanel()
    {
        int w = pnlStatus.ClientSize.Width;
        int h = pnlStatus.ClientSize.Height;

        lblStatusTitle.SetBounds(16, 4, 180, 18);
        lblStatusMessage.SetBounds(16, 24, Math.Max(100, w - 32), Math.Max(18, h - 28));
    }


    private void WireEvents()
    {
        btnAddSource.Click += BtnAddSource_Click;
        btnRemoveSource.Click += BtnRemoveSource_Click;
        btnClearSources.Click += BtnClearSources_Click;

        btnBrowseTarget.Click += BtnSelectTarget_Click;
        btnSelectTarget.Click += BtnSelectTarget_Click;
        btnOpenTarget.Click += BtnOpenTarget_Click;
    }

    private void BtnAddSource_Click(object? sender, EventArgs e)
    {
        IReadOnlyList<string> selectedFolders;

        try
        {
            selectedFolders = NativeFolderPicker.SelectFolders(this, "Select source root folder(s)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "The multi-folder picker could not be opened. FileForge will fall back to single-folder selection.\n\n" + ex.Message,
                "FileForge",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            selectedFolders = SelectSingleSourceFolderFallback();
        }

        if (selectedFolders.Count == 0)
        {
            SetStatus("Ready", "Source folder selection cancelled.");
            return;
        }

        int beforeCount = lstSourceFolders.Items.Count;

        foreach (string folder in selectedFolders)
            AddSourceFolder(folder, showIndividualStatus: false);

        int addedCount = lstSourceFolders.Items.Count - beforeCount;

        if (addedCount == 0)
            SetStatus("Ready", "No new source folders added. Selected folder(s) may already exist in the list.");
        else
            SetStatus("Ready", $"Added {addedCount:N0} source folder(s). Use Ctrl/Shift in the list to select multiple entries for removal.");
    }

    private void BtnRemoveSource_Click(object? sender, EventArgs e)
    {
        if (lstSourceFolders.SelectedIndices.Count == 0)
        {
            SetStatus("Ready", "Select one or more source folders to remove.");
            return;
        }

        int removedCount = lstSourceFolders.SelectedIndices.Count;

        for (int i = lstSourceFolders.SelectedIndices.Count - 1; i >= 0; i--)
        {
            int index = lstSourceFolders.SelectedIndices[i];
            lstSourceFolders.Items.RemoveAt(index);
        }

        UpdateSourceCount();
        SetStatus("Ready", $"Removed {removedCount:N0} source folder(s).");
    }

    private void BtnClearSources_Click(object? sender, EventArgs e)
    {
        if (lstSourceFolders.Items.Count == 0)
        {
            SetStatus("Ready", "Source folder list is already empty.");
            return;
        }

        lstSourceFolders.Items.Clear();
        UpdateSourceCount();
        SetStatus("Ready", "All source folders cleared.");
    }

    private void BtnSelectTarget_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select target master folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(txtTargetFolder.Text) && Directory.Exists(txtTargetFolder.Text))
            dialog.SelectedPath = txtTargetFolder.Text;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        txtTargetFolder.Text = NormalizeFolderPath(dialog.SelectedPath);
        SetStatus("Ready", "Target master folder selected.");
    }

    private void BtnOpenTarget_Click(object? sender, EventArgs e)
    {
        string targetPath = txtTargetFolder.Text.Trim();

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            SetStatus("Ready", "No target folder selected.");
            return;
        }

        if (!Directory.Exists(targetPath))
        {
            MessageBox.Show(this, "The selected target folder does not exist.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetStatus("Ready", "Target folder does not exist.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = targetPath,
            UseShellExecute = true
        });

        SetStatus("Ready", "Target folder opened.");
    }

    private void AddSourceFolder(string folderPath, bool showIndividualStatus = true)
    {
        string normalizedPath = NormalizeFolderPath(folderPath);

        if (!Directory.Exists(normalizedPath))
        {
            MessageBox.Show(this, "The selected source folder does not exist.", "FileForge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetStatus("Ready", "Source folder does not exist.");
            return;
        }

        foreach (object item in lstSourceFolders.Items)
        {
            string existingPath = item.ToString() ?? string.Empty;
            if (string.Equals(existingPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                if (showIndividualStatus)
                    SetStatus("Ready", "Source folder is already in the list.");
                return;
            }
        }

        lstSourceFolders.Items.Add(normalizedPath);
        UpdateSourceCount();
        if (showIndividualStatus)
            SetStatus("Ready", "Source root folder added.");
    }

    private IReadOnlyList<string> SelectSingleSourceFolderFallback()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a source root folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return Array.Empty<string>();

        return new[] { dialog.SelectedPath };
    }

    private static class NativeFolderPicker
    {
        private const int S_OK = 0;
        private const uint ERROR_CANCELLED = 0x800704C7;
        private static readonly Guid FileOpenDialogClsid = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");

        public static IReadOnlyList<string> SelectFolders(IWin32Window owner, string title)
        {
            Type? dialogType = Type.GetTypeFromCLSID(FileOpenDialogClsid);
            if (dialogType == null)
                throw new InvalidOperationException("Windows File Open Dialog COM type is not available.");

            object? dialogObject = Activator.CreateInstance(dialogType);
            if (dialogObject == null)
                throw new InvalidOperationException("Windows File Open Dialog could not be created.");

            IFileOpenDialog? dialog = null;
            IShellItemArray? results = null;

            try
            {
                dialog = (IFileOpenDialog)dialogObject;

                dialog.GetOptions(out FileOpenOptions options);
                dialog.SetOptions(options
                    | FileOpenOptions.PickFolders
                    | FileOpenOptions.AllowMultiSelect
                    | FileOpenOptions.ForceFileSystem
                    | FileOpenOptions.PathMustExist);

                dialog.SetTitle(title);
                dialog.SetOkButtonLabel("Add Selected Folders");

                int hr = dialog.Show(owner.Handle);

                if ((uint)hr == ERROR_CANCELLED)
                    return Array.Empty<string>();

                if (hr != S_OK)
                    Marshal.ThrowExceptionForHR(hr);

                dialog.GetResults(out results);
                results.GetCount(out uint count);

                List<string> selectedPaths = new();

                for (uint i = 0; i < count; i++)
                {
                    results.GetItemAt(i, out IShellItem item);
                    IntPtr pathPointer = IntPtr.Zero;

                    try
                    {
                        item.GetDisplayName(ShellItemDisplayName.FileSystemPath, out pathPointer);
                        string? path = Marshal.PtrToStringUni(pathPointer);

                        if (!string.IsNullOrWhiteSpace(path))
                            selectedPaths.Add(path);
                    }
                    finally
                    {
                        if (pathPointer != IntPtr.Zero)
                            Marshal.FreeCoTaskMem(pathPointer);

                        if (Marshal.IsComObject(item))
                            Marshal.FinalReleaseComObject(item);
                    }
                }

                return selectedPaths;
            }
            finally
            {
                if (results != null && Marshal.IsComObject(results))
                    Marshal.FinalReleaseComObject(results);

                if (dialog != null && Marshal.IsComObject(dialog))
                    Marshal.FinalReleaseComObject(dialog);
                else if (Marshal.IsComObject(dialogObject))
                    Marshal.FinalReleaseComObject(dialogObject);
            }
        }

        [Flags]
        private enum FileOpenOptions : uint
        {
            PickFolders = 0x00000020,
            ForceFileSystem = 0x00000040,
            AllowMultiSelect = 0x00000200,
            PathMustExist = 0x00000800
        }

        private enum ShellItemDisplayName : uint
        {
            FileSystemPath = 0x80058000
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
            void SetOptions(FileOpenOptions fos);
            void GetOptions(out FileOpenOptions pfos);
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
            void AddPlace(IShellItem psi, int fdap);
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
            void GetDisplayName(ShellItemDisplayName sigdnName, out IntPtr ppszName);
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
            void GetPropertyDescriptionList(ref Guid keyType, ref Guid riid, out IntPtr ppv);
            void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);
            void GetCount(out uint pdwNumItems);
            void GetItemAt(uint dwIndex, out IShellItem ppsi);
            void EnumItems(out IntPtr ppenumShellItems);
        }
    }

    private static string NormalizeFolderPath(string folderPath)
    {
        string fullPath = Path.GetFullPath(folderPath.Trim());
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private void UpdateSourceCount()
    {
        lblSources.Text = lstSourceFolders.Items.Count.ToString("N0");
    }

    private void SetStatus(string title, string message)
    {
        lblStatusTitle.Text = title;
        lblStatusMessage.Text = message;
    }

    private void SetStatValues(int sources, int files, int unique, int duplicates, int conflicts)
    {
        lblSources.Text = sources.ToString("N0");
        lblFiles.Text = files.ToString("N0");
        lblUnique.Text = unique.ToString("N0");
        lblDuplicates.Text = duplicates.ToString("N0");
        lblConflicts.Text = conflicts.ToString("N0");
    }
}
