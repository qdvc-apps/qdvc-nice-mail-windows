using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

internal sealed class PhrasesTab : TabView
{
    private readonly ToolStrip _toolbar;
    private readonly ToolStripButton _add, _edit, _delete, _copy;
    private readonly ToolStripTextBox _search;
    private readonly ListView _list;

    private List<Phrase> _phrases = new();

    public PhrasesTab(Preferences prefs) : base(prefs)
    {
        _toolbar = Ui.NewToolStrip(this);
        _add = Ui.Button(this, "Add", Glyph.Add, (_, _) => AddPhrase(), "Add a phrase");
        _edit = Ui.Button(this, "Edit", Glyph.Edit, (_, _) => EditPhrase(), "Edit the selected phrase (Enter)");
        _delete = Ui.Button(this, "Delete", Glyph.Delete, (_, _) => DeletePhrase(), "Delete the selected phrase (Del)");
        _copy = Ui.Button(this, "Copy", Glyph.Copy, (_, _) => CopyToClipboard(), "Copy (Ctrl+C)");
        _toolbar.Items.AddRange(new ToolStripItem[] { _add, _edit, _delete, new ToolStripSeparator(), _copy });

        var (searchRow, search) = Ui.SearchRow(this, (_, _) => Rebuild());
        _search = search;

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            ShowItemToolTips = true,
        };
        _list.Columns.Add("Phrase", Ui.Scale(this, 600));
        Ui.SetRowHeight(_list, Ui.Scale(this, 24));
        _list.SelectedIndexChanged += (_, _) => UpdateButtons();
        _list.ItemActivate += (_, _) => EditPhrase();
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) { DeletePhrase(); e.Handled = true; }
        };
        _list.Resize += (_, _) => FitColumn();

        Controls.Add(_list);
        Controls.Add(searchRow);
        Controls.Add(_toolbar);

        ApplyPreferences();
    }

    public override string Title => "Phrases";
    public override bool HasSearch => true;
    public override void FocusSearch() { _search.Focus(); _search.SelectAll(); }

    public override void ApplyPreferences() => Ui.ApplyToolbarStyle(_toolbar, Prefs.ToolbarStyle);

    public override void Reload()
    {
        _phrases = new List<Phrase>();
        if (Workspace is not null)
        {
            try { _phrases = Workspace.LoadPhrases(); }
            catch (IOException ex) { ShowError("Loading phrases", ex); }
        }
        Rebuild();
    }

    private void FitColumn()
    {
        if (_list.Columns.Count > 0)
            _list.Columns[0].Width = Math.Max(Ui.Scale(this, 200), _list.ClientSize.Width - 4);
    }

    private static string OneLine(string s) => s.Replace("\r\n", "\n").Replace("\n", " ↵ ");

    private void Rebuild(string? selectId = null)
    {
        selectId ??= Selected?.Id;
        string q = _search.Text.Trim();
        var visible = _phrases
            .Where(p => q.Length == 0 || p.Text.Contains(q, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(p => p.Text, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var p in visible)
            {
                var item = new ListViewItem(OneLine(p.Text)) { Tag = p, ToolTipText = p.Text };
                _list.Items.Add(item);
                if (p.Id == selectId) { item.Selected = true; item.Focused = true; }
            }
            if (_list.SelectedItems.Count > 0) _list.SelectedItems[0].EnsureVisible();
        }
        finally
        {
            _list.EndUpdate();
        }
        FitColumn();
        UpdateButtons();
    }

    private Phrase? Selected => _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as Phrase : null;

    private void UpdateButtons()
    {
        bool sel = Selected is not null;
        _add.Enabled = Workspace is not null;
        _edit.Enabled = _delete.Enabled = sel && Workspace is not null;
        _copy.Enabled = sel;
    }

    private bool Save()
    {
        if (Workspace is null) return false;
        try { Workspace.SavePhrases(_phrases); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Saving phrases", ex);
            return false;
        }
    }

    public override void CopyToClipboard()
    {
        if (Selected is not { } p) { Status("Select a phrase to copy."); return; }
        if (Ui.TrySetClipboard(Signature.ToCrLf(p.Text))) Status("Copied phrase");
    }

    private void AddPhrase()
    {
        if (!RequireWorkspace()) return;
        using var dlg = new TextPromptDialog("Add Phrase", "Phrase (Ctrl+Enter to save):", "", multiline: true);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        string text = Signature.Normalise(dlg.Value);
        if (text.Length == 0) return;
        var phrase = new Phrase { Id = Workspace.NextPhraseId(_phrases), Text = text };
        _phrases.Add(phrase);
        if (Save()) { _search.Clear(); Rebuild(phrase.Id); Status("Phrase added"); }
    }

    private void EditPhrase()
    {
        if (Selected is not { } p || !RequireWorkspace()) return;
        using var dlg = new TextPromptDialog("Edit Phrase", "Phrase (Ctrl+Enter to save):", p.Text, multiline: true);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        string text = Signature.Normalise(dlg.Value);
        if (text.Length == 0) return;
        p.Text = text;
        if (Save()) { Rebuild(p.Id); Status("Phrase updated"); }
    }

    private void DeletePhrase()
    {
        if (Selected is not { } p || !RequireWorkspace()) return;
        string preview = p.Text.Length > 80 ? p.Text[..80] + "…" : p.Text;
        if (MessageBox.Show(this, $"Delete this phrase?\n\n{preview}", "Delete Phrase",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        _phrases.Remove(p);
        if (Save()) { Rebuild(); Status("Phrase deleted"); }
    }
}
