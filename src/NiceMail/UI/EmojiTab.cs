using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

internal sealed class EmojiTab : TabView
{
    private sealed record Row(EmojiInfo Info, Favourite? Favourite);

    private static readonly Lazy<EmojiCatalogue> Catalogue = new(EmojiCatalogue.LoadEmbedded);

    private readonly ToolStrip _toolbar;
    private readonly ToolStripComboBox _block;
    private readonly ToolStripButton _addCustom, _moveUp, _moveDown, _copy;
    private readonly ToolStripTextBox _search;
    private readonly EmojiListView _list;
    private readonly ContextMenuStrip _menu;

    private List<Favourite> _favourites = new();
    private List<Row> _rows = new();

    public EmojiTab(Preferences prefs) : base(prefs)
    {
        _toolbar = Ui.NewToolStrip(this);
        _block = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, ToolTipText = "Emoji block" };
        _block.Items.AddRange(new object[] { "Favourites", "All Emoji" });
        _block.SelectedIndex = 0;
        _block.SelectedIndexChanged += (_, _) => Rebuild();
        _addCustom = Ui.Button(this, "Add Custom", Glyph.Add, (_, _) => AddCustom(),
            "Paste an emoji that isn't in the list");
        _moveUp = Ui.Button(this, "Move Up", Glyph.Up, (_, _) => MoveFavourite(-1));
        _moveDown = Ui.Button(this, "Move Down", Glyph.Down, (_, _) => MoveFavourite(+1));
        _copy = Ui.Button(this, "Copy", Glyph.Copy, (_, _) => CopyToClipboard(), "Copy (Ctrl+C)");
        _toolbar.Items.AddRange(new ToolStripItem[]
        {
            _block, new ToolStripSeparator(), _addCustom, _moveUp, _moveDown, new ToolStripSeparator(), _copy,
        });

        var (searchRow, search) = Ui.SearchRow(this, (_, _) => Rebuild());
        _search = search;

        _list = new EmojiListView
        {
            Dock = DockStyle.Fill,
            MultiSelect = false,
            HideSelection = false,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
        };
        _list.Columns.Add("Emoji", Ui.Scale(this, 64));
        _list.Columns.Add("Name", Ui.Scale(this, 340));
        _list.Columns.Add("Label", Ui.Scale(this, 180));
        Ui.SetRowHeight(_list, Ui.Scale(this, 30));
        _list.SelectedIndexChanged += (_, _) => UpdateButtons();

        _menu = new ContextMenuStrip();
        _menu.Opening += (_, e) => e.Cancel = !BuildContextMenu();
        _list.ContextMenuStrip = _menu;

        // Dock order: the last control added is docked first (topmost).
        Controls.Add(_list);
        Controls.Add(searchRow);
        Controls.Add(_toolbar);

        ApplyPreferences();
    }

    public override string Title => "Emoji";
    public override bool HasSearch => true;
    public override void FocusSearch() { _search.Focus(); _search.SelectAll(); }

    private bool ShowingFavourites => _block.SelectedIndex == 0;

    public override void ApplyPreferences()
    {
        Ui.ApplyToolbarStyle(_toolbar, Prefs.ToolbarStyle);
        Rebuild(); // skin tone may have changed
    }

    public override void Reload()
    {
        _favourites = new List<Favourite>();
        if (Workspace is not null)
        {
            try { _favourites = Workspace.LoadFavourites(); }
            catch (IOException ex) { ShowError("Loading favourites", ex); }
        }
        Rebuild();
    }

    // ---- Model → view ------------------------------------------------

    private EmojiInfo? Resolve(Favourite f) =>
        f.IsCustom
            ? (f.Char.Length > 0 ? new EmojiInfo { Id = f.Id, Char = f.Char, Name = "Custom emoji", IsCustom = true } : null)
            : Catalogue.Value.Find(f.Id);

    private void Rebuild(string? selectId = null)
    {
        selectId ??= Selected?.Info.Id;
        var favById = _favourites.ToDictionary(f => f.Id, StringComparer.Ordinal);

        IEnumerable<Row> rows;
        if (ShowingFavourites)
        {
            rows = _favourites.Select(f => (f, info: Resolve(f)))
                              .Where(x => x.info is not null)
                              .Select(x => new Row(x.info!, x.f));
        }
        else
        {
            var custom = _favourites.Where(f => f.IsCustom).Select(f => (f, info: Resolve(f)))
                                    .Where(x => x.info is not null)
                                    .Select(x => new Row(x.info!, x.f));
            rows = Catalogue.Value.All.Select(e => new Row(e, favById.GetValueOrDefault(e.Id))).Concat(custom);
        }

        string q = _search.Text.Trim();
        if (q.Length > 0)
        {
            rows = rows.Where(r =>
                r.Info.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                r.Info.Id.Replace('_', ' ').Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                (r.Favourite?.Label.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
        }
        _rows = rows.ToList();

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            var items = new ListViewItem[_rows.Count];
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                string name = r.Info.IsCustom ? "Custom emoji" : r.Info.Name;
                if (!ShowingFavourites && r.Favourite is not null && !r.Info.IsCustom) name += "  ★";
                var item = new ListViewItem(r.Info.WithTone(Prefs.SkinTone)) { Tag = r };
                item.SubItems.Add(name);
                item.SubItems.Add(r.Favourite?.Label ?? "");
                items[i] = item;
            }
            _list.Items.AddRange(items);
            if (selectId is not null)
            {
                int idx = _rows.FindIndex(r => r.Info.Id == selectId);
                if (idx >= 0)
                {
                    _list.Items[idx].Selected = true;
                    _list.Items[idx].Focused = true;
                    _list.Items[idx].EnsureVisible();
                }
            }
        }
        finally
        {
            _list.EndUpdate();
        }
        UpdateButtons();
    }

    private Row? Selected => _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as Row : null;

    private void UpdateButtons()
    {
        var sel = Selected;
        bool favMode = ShowingFavourites && Workspace is not null;
        _addCustom.Enabled = Workspace is not null;
        _copy.Enabled = sel is not null;
        _moveUp.Enabled = favMode && sel is not null && _list.SelectedIndices[0] > 0;
        _moveDown.Enabled = favMode && sel is not null && _list.SelectedIndices[0] < _rows.Count - 1;
    }

    // ---- Commands ----------------------------------------------------

    public override void CopyToClipboard()
    {
        if (Selected is not { } row) { Status("Select an emoji to copy."); return; }
        string glyph = row.Info.WithTone(Prefs.SkinTone);
        if (Ui.TrySetClipboard(glyph)) Status($"Copied {glyph}");
    }

    private bool Save()
    {
        if (Workspace is null) return false;
        try { Workspace.SaveFavourites(_favourites); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Saving favourites", ex);
            return false;
        }
    }

    private void MoveFavourite(int delta)
    {
        if (!ShowingFavourites || Selected?.Favourite is not { } fav) return;
        int viewIdx = _list.SelectedIndices[0];
        int targetViewIdx = viewIdx + delta;
        if (targetViewIdx < 0 || targetViewIdx >= _rows.Count || _rows[targetViewIdx].Favourite is not { } other) return;

        // Swap within the underlying list, so moving works even while a search filter is active.
        int a = _favourites.IndexOf(fav), b = _favourites.IndexOf(other);
        (_favourites[a], _favourites[b]) = (_favourites[b], _favourites[a]);
        if (Save()) Rebuild(fav.Id);
    }

    private void ToggleFavourite(Row row)
    {
        if (!RequireWorkspace()) return;
        if (row.Favourite is { } fav)
        {
            if (fav.IsCustom && MessageBox.Show(this,
                    $"Remove the custom emoji {row.Info.Char}? It will no longer appear in any list.",
                    "Remove Custom Emoji", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;
            _favourites.Remove(fav);
            if (Save()) { Rebuild(); Status($"Removed {row.Info.Char} from favourites"); }
        }
        else
        {
            _favourites.Add(new Favourite { Id = row.Info.Id });
            if (Save()) { Rebuild(row.Info.Id); Status($"Added {row.Info.Char} to favourites"); }
        }
    }

    private void SetLabel(Row row)
    {
        if (row.Favourite is not { } fav) return;
        using var dlg = new TextPromptDialog("Set Label", $"Label for {row.Info.Char}:", fav.Label);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        fav.Label = dlg.Value.Trim().Replace("\r", " ").Replace("\n", " ");
        if (Save()) Rebuild(fav.Id);
    }

    private void AddCustom()
    {
        if (!RequireWorkspace()) return;
        using var dlg = new TextPromptDialog("Add Custom Emoji", "Paste an emoji:", "");
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        string glyph = dlg.Value.Trim();
        if (glyph.Length == 0) return;

        string id;
        var known = Catalogue.Value.FindByChar(glyph);
        if (known is not null)
        {
            id = known.Id;
            if (!_favourites.Any(f => f.Id == id)) _favourites.Add(new Favourite { Id = id });
            Status($"{known.Char} is already in the catalogue ({known.Name}); added to favourites.");
        }
        else
        {
            id = EmojiCatalogue.CustomId(glyph);
            if (!_favourites.Any(f => f.Id == id)) _favourites.Add(new Favourite { Id = id, Char = glyph });
            Status($"Added custom emoji {glyph}");
        }
        if (Save()) Rebuild(id);
    }

    private bool BuildContextMenu()
    {
        _menu.Items.Clear();
        if (Selected is not { } row) return false;

        _menu.Items.Add("&Copy", Icons.Get(Glyph.Copy, Ui.Scale(this, 16)), (_, _) => CopyToClipboard());
        _menu.Items.Add(new ToolStripSeparator());
        var toggle = _menu.Items.Add(row.Favourite is null ? "&Add to Favourites" : "&Remove from Favourites",
            null, (_, _) => ToggleFavourite(row));
        toggle.Enabled = Workspace is not null;
        var label = _menu.Items.Add("Set &Label…", Icons.Get(Glyph.Tag, Ui.Scale(this, 16)), (_, _) => SetLabel(row));
        label.Enabled = row.Favourite is not null;
        if (ShowingFavourites)
        {
            _menu.Items.Add(new ToolStripSeparator());
            var up = _menu.Items.Add("Move &Up", Icons.Get(Glyph.Up, Ui.Scale(this, 16)), (_, _) => MoveFavourite(-1));
            var down = _menu.Items.Add("Move &Down", Icons.Get(Glyph.Down, Ui.Scale(this, 16)), (_, _) => MoveFavourite(+1));
            up.Enabled = _moveUp.Enabled;
            down.Enabled = _moveDown.Enabled;
        }
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _menu.Dispose();
        base.Dispose(disposing);
    }
}
