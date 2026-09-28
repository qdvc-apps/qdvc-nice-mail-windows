using System.Reflection;
using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

internal sealed class MainForm : Form
{
    private const string AppName = "QDVC Nice Mail";

    private readonly Preferences _prefs;
    private readonly PageTabStrip _tabs;
    private readonly Panel _pages;
    private readonly List<TabView> _views;
    private readonly ToolStripStatusLabel _statusMessage, _statusWorkspace;
    private readonly ToolStripMenuItem _findItem, _newRefItem;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private Workspace? _workspace;

    public MainForm(Preferences prefs, string? workspacePath)
    {
        _prefs = prefs;
        Text = AppName;
        AutoScaleMode = AutoScaleMode.None;
        MinimumSize = new Size(Ui.Scale(this, 520), Ui.Scale(this, 380));
        StartPosition = FormStartPosition.WindowsDefaultLocation;
        Size = prefs.WindowWidth > 0 && prefs.WindowHeight > 0
            ? new Size(prefs.WindowWidth, prefs.WindowHeight)
            : new Size(Ui.Scale(this, 760), Ui.Scale(this, 600));
        if (prefs.WindowMaximised) WindowState = FormWindowState.Maximized;
        LoadBrandingIcon();

        // ---- Menu ------------------------------------------------------
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(new ToolStripMenuItem("&Open Workspace…", null, (_, _) => BrowseForWorkspace(), Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("&Copy", Icons.Get(Glyph.Copy, Ui.Scale(this, 16)), (_, _) => DoCopy(), Keys.Control | Keys.C));
        _findItem = new ToolStripMenuItem("&Find", null, (_, _) => Active?.FocusSearch(), Keys.Control | Keys.F);
        edit.DropDownItems.Add(_findItem);
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(new ToolStripMenuItem("&Preferences…", null, (_, _) => ShowPreferences())
        {
            ShortcutKeys = Keys.Control | Keys.Oemcomma,
            ShortcutKeyDisplayString = "Ctrl+,",
        });

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(new ToolStripMenuItem("&Refresh", Icons.Get(Glyph.Refresh, Ui.Scale(this, 16)), (_, _) => Active?.Reload(), Keys.Control | Keys.R));
        _newRefItem = new ToolStripMenuItem("&New Message Ref", null, (_, _) => Active?.NewMessageRef(), Keys.F5);
        view.DropDownItems.Add(_newRefItem);

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(new ToolStripMenuItem("&About " + AppName, null, (_, _) => ShowAbout()));

        menu.Items.AddRange(new ToolStripItem[] { file, edit, view, help });
        MainMenuStrip = menu;

        // ---- Tabs --------------------------------------------------------
        _views = new List<TabView>
        {
            new EmojiTab(prefs), new PhrasesTab(prefs), new SignatureTab(prefs), new NoteTab(prefs),
        };
        _tabs = new PageTabStrip();
        _pages = new Panel { Dock = DockStyle.Fill };
        foreach (var v in _views)
        {
            _tabs.AddTab(v.Title);
            v.Visible = false;
            _pages.Controls.Add(v);
            v.StatusMessage += (_, msg) => ShowStatus(msg);
        }
        _tabs.SelectedIndexChanged += (_, _) => ShowPage();

        // ---- Status bar --------------------------------------------------
        var status = new StatusStrip { SizingGrip = true };
        _statusMessage = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusWorkspace = new ToolStripStatusLabel { TextAlign = ContentAlignment.MiddleRight };
        status.Items.AddRange(new ToolStripItem[] { _statusMessage, _statusWorkspace });
        _statusTimer = new System.Windows.Forms.Timer { Interval = 4000 };
        _statusTimer.Tick += (_, _) => { _statusMessage.Text = ""; _statusTimer.Stop(); };

        // Dock order: the last control added is docked first (outermost).
        Controls.Add(_pages);
        Controls.Add(_tabs);
        Controls.Add(status);
        Controls.Add(menu);

        OpenWorkspace(workspacePath, quiet: true);
        _tabs.SelectedIndex = 0;
    }

    /// <summary>Uses branding/app.ico if it was embedded at build time; otherwise keeps the default.</summary>
    private void LoadBrandingIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Qdvc.NiceMail.app.ico");
        if (stream is null) return;
        try { Icon = new Icon(stream); }
        catch (ArgumentException) { /* not a valid .ico; keep the default icon */ }
    }

    private TabView? Active => _tabs.SelectedIndex >= 0 ? _views[_tabs.SelectedIndex] : null;

    private void ShowPage()
    {
        var active = Active;
        _pages.SuspendLayout();
        foreach (var v in _views) v.Visible = ReferenceEquals(v, active);
        _pages.ResumeLayout();
        UpdateMenus();
        if (IsHandleCreated) active?.FocusDefault();
    }

    /// <summary>Tab-switching shortcuts that TabControl used to provide.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Tab:
            case Keys.Control | Keys.PageDown:
                _tabs.Cycle(+1);
                return true;
            case Keys.Control | Keys.Shift | Keys.Tab:
            case Keys.Control | Keys.PageUp:
                _tabs.Cycle(-1);
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Active?.FocusDefault();
    }

    private void UpdateMenus()
    {
        _findItem.Enabled = Active?.HasSearch ?? false;
        _newRefItem.Enabled = Active?.HasMessageRef ?? false;
    }

    private void ShowStatus(string message)
    {
        _statusMessage.Text = message;
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    /// <summary>
    /// Ctrl+C is bound to Edit → Copy, which would otherwise swallow the shortcut
    /// inside text boxes. If a text box has a selection, copy that; otherwise the
    /// active tab decides what to copy.
    /// </summary>
    private void DoCopy()
    {
        if (Ui.FocusedControl() is TextBoxBase tb && tb.SelectionLength > 0)
        {
            Ui.TrySetClipboard(tb.SelectedText);
            return;
        }
        Active?.CopyToClipboard();
    }

    // ---- Workspace -------------------------------------------------------

    private void BrowseForWorkspace()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose a workspace folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            InitialDirectory = _workspace?.Root ?? _prefs.LastWorkspace ?? "",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) OpenWorkspace(dlg.SelectedPath, quiet: false);
    }

    private void OpenWorkspace(string? path, bool quiet)
    {
        Workspace? ws = null;
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                ws = new Workspace(path);
                ws.EnsureLayout();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                if (!quiet)
                    MessageBox.Show(this, $"Couldn't open the workspace:\n\n{ex.Message}", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                ws = null;
            }
        }

        _workspace = ws;
        foreach (var v in _views) v.SetWorkspace(ws);

        if (ws is not null)
        {
            _prefs.LastWorkspace = ws.Root;
            _prefs.Save();
            Text = $"{Path.GetFileName(ws.Root.TrimEnd(Path.DirectorySeparatorChar))} — {AppName}";
            _statusWorkspace.Text = ws.Root;
            if (!quiet) ShowStatus("Workspace opened");
        }
        else
        {
            Text = AppName;
            _statusWorkspace.Text = "No workspace — File → Open Workspace… (Ctrl+O)";
        }
    }

    // ---- Dialogs ---------------------------------------------------------

    private void ShowPreferences()
    {
        using var dlg = new PreferencesDialog(_prefs);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        dlg.ApplyTo(_prefs);
        _prefs.Save();
        foreach (var v in _views) v.ApplyPreferences();
    }

    private void ShowAbout()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "";
        MessageBox.Show(this,
            $"{AppName} {version}\n\nEmoji, phrases, and a plaintext signature, one click away.\n\n" +
            "Windows Forms port of the QDVC Python/GTK app.",
            "About " + AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _prefs.WindowMaximised = WindowState == FormWindowState.Maximized;
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _prefs.WindowWidth = bounds.Width;
        _prefs.WindowHeight = bounds.Height;
        _prefs.Save();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _statusTimer.Dispose();
        base.Dispose(disposing);
    }
}
