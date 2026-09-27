using System.Text;
using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

internal sealed class NoteTab : TabView
{
    private readonly ToolStrip _toolbar;
    private readonly TextBox _address, _subject, _body;
    private readonly Label _callout;
    private readonly Panel _calloutPanel;
    private string _messageRef = MessageRef.New();
    private Font? _ownedFont;

    public NoteTab(Preferences prefs) : base(prefs)
    {
        _toolbar = Ui.NewToolStrip(this);
        var send = Ui.Button(this, "Send", Glyph.Save, (_, _) => Send(), "Save the note as a self-addressed .eml");
        var newRef = Ui.Button(this, "New Ref", Glyph.Refresh, (_, _) => NewMessageRef(), "Generate a new message ref (F5)");
        _toolbar.Items.AddRange(new ToolStripItem[] { send, new ToolStripSeparator(), newRef });

        int gap = Ui.Scale(this, 6);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(Ui.Scale(this, 12)),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label Caption(string text) => new()
        {
            Text = text, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0, gap + Ui.Scale(this, 3), gap * 2, gap),
        };

        _address = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, gap, 0, gap), Text = Prefs.NoteAddress };
        _address.TextChanged += (_, _) => Prefs.NoteAddress = _address.Text.Trim();
        _subject = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, gap, 0, gap) };
        _body = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = false,
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0, gap, 0, gap),
        };

        _callout = new Label { AutoSize = true, Dock = DockStyle.Fill };
        _calloutPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(Ui.Scale(this, 10), Ui.Scale(this, 8), Ui.Scale(this, 10), Ui.Scale(this, 8)),
            Margin = new Padding(0, gap, 0, gap),
        };
        _calloutPanel.Controls.Add(_callout);
        ApplyCalloutColours();

        grid.Controls.Add(Caption("&From / To:"), 0, 0);
        grid.Controls.Add(_address, 1, 0);
        grid.Controls.Add(Caption("S&ubject:"), 0, 1);
        grid.Controls.Add(_subject, 1, 1);
        grid.Controls.Add(_calloutPanel, 1, 2);
        grid.Controls.Add(Caption("&Body:"), 0, 3);
        grid.Controls.Add(_body, 1, 3);

        Controls.Add(grid);
        Controls.Add(_toolbar);

        UpdateCallout();
        ApplyPreferences();
    }

    public override string Title => "Note to Self";
    public override bool HasMessageRef => true;

    private void ApplyCalloutColours()
    {
        if (SystemInformation.HighContrast)
        {
            _calloutPanel.BackColor = SystemColors.Info;
            _callout.ForeColor = SystemColors.InfoText;
        }
        else if (Application.IsDarkModeEnabled)
        {
            _calloutPanel.BackColor = Color.FromArgb(0x1E, 0x3A, 0x24);
            _callout.ForeColor = Color.FromArgb(0xB8, 0xE6, 0xBF);
        }
        else
        {
            _calloutPanel.BackColor = Color.FromArgb(0xDF, 0xF6, 0xDD);
            _callout.ForeColor = Color.FromArgb(0x0E, 0x5A, 0x1E);
        }
    }

    private void UpdateCallout() =>
        _callout.Text = $"Message ref. {_messageRef} will be attached below an m-dash.";

    public override void ApplyPreferences()
    {
        Ui.ApplyToolbarStyle(_toolbar, Prefs.ToolbarStyle);
        var old = _ownedFont;
        _ownedFont = Ui.SignatureFont(Prefs);
        _address.Font = _subject.Font = _body.Font = _ownedFont;
        old?.Dispose();
    }

    public override void NewMessageRef()
    {
        _messageRef = MessageRef.New();
        UpdateCallout();
        Status($"New message ref: {_messageRef}");
    }

    /// <summary>The note isn't stored in the workspace, so a refresh just regenerates the ref.</summary>
    public override void Reload()
    {
        _messageRef = MessageRef.New();
        UpdateCallout();
    }

    public override void CopyToClipboard()
    {
        Status("Select text in a field to copy it.");
    }

    private void Send()
    {
        string address = _address.Text.Trim();
        if (address.Length == 0 || !address.Contains('@'))
        {
            MessageBox.Show(this, "Please enter a valid From / To email address.", "Note to Self",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _address.Focus();
            return;
        }
        if (_subject.Text.Trim().Length == 0 && _body.Text.Trim().Length == 0)
        {
            MessageBox.Show(this, "The note is empty. Add a subject or some body text first.", "Note to Self",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _subject.Focus();
            return;
        }

        var now = DateTimeOffset.Now;
        using var dlg = new SaveFileDialog
        {
            Title = "Save Note to Self",
            Filter = "Email message (*.eml)|*.eml|All files (*.*)|*.*",
            DefaultExt = "eml",
            AddExtension = true,
            FileName = Eml.DefaultFileName(now.LocalDateTime, _messageRef),
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string eml = Eml.Build(address, _subject.Text.Trim(), _body.Text, _messageRef, now);
            File.WriteAllText(dlg.FileName, eml, new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Saving the note", ex);
            return;
        }

        _subject.Clear();
        _body.Clear();
        _messageRef = MessageRef.New();
        UpdateCallout();
        _subject.Focus();
        Status($"Saved {Path.GetFileName(dlg.FileName)}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _ownedFont?.Dispose();
    }
}
