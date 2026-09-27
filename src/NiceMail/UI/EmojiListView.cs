namespace Qdvc.NiceMail.UI;

/// <summary>
/// A details-view ListView whose first column is drawn with <see cref="ColorEmoji"/>
/// so emoji appear in colour. All columns are owner-drawn so that selection
/// colours are consistent across the row, using system colours so the list
/// follows light and dark mode.
/// </summary>
internal sealed class EmojiListView : ListView
{
    private ListViewItem? _hover;

    /// <summary>Monochrome fallback font if Direct2D is unavailable.</summary>
    public Font FallbackEmojiFont { get; set; } = new("Segoe UI Emoji", 14f);

    public EmojiListView()
    {
        OwnerDraw = true;
        DoubleBuffered = true;
        View = View.Details;
        FullRowSelect = true;
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        e.DrawDefault = true;
        base.OnDrawColumnHeader(e);
    }

    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        // Everything is painted per sub-item in OnDrawSubItem.
        e.DrawDefault = false;
        base.OnDrawItem(e);
    }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null || e.SubItem is null) return;

        // For column 0, e.Bounds spans the whole row; clip it to the column.
        var bounds = e.Bounds;
        if (e.ColumnIndex == 0) bounds.Width = Columns[0].Width;

        bool selected = e.Item.Selected;
        bool active = Focused;
        Color back = selected ? (active ? SystemColors.Highlight : SystemColors.Control) : BackColor;
        Color fore = selected ? (active ? SystemColors.HighlightText : SystemColors.ControlText) : ForeColor;

        if (e.ColumnIndex == 0)
        {
            ColorEmoji.Draw(e.Graphics, e.SubItem.Text, bounds, back, fore, FallbackEmojiFont);
        }
        else
        {
            using (var b = new SolidBrush(back)) e.Graphics.FillRectangle(b, bounds);
            var textBounds = Rectangle.Inflate(bounds, -Ui.Scale(this, 6), 0);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font, textBounds, fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        if (e.ColumnIndex == Columns.Count - 1 && e.Item.Focused && active && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, e.Item.Bounds, fore, back);

        base.OnDrawSubItem(e);
    }

    // With OwnerDraw + FullRowSelect, the native control only invalidates the
    // first sub-item on hover, which leaves stale highlight in the other columns.
    protected override void OnMouseMove(MouseEventArgs e)
    {
        var item = GetItemAt(e.X, e.Y);
        if (!ReferenceEquals(item, _hover))
        {
            if (_hover is not null && _hover.ListView == this) Invalidate(_hover.Bounds);
            if (item is not null) Invalidate(item.Bounds);
            _hover = item;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_hover is not null && _hover.ListView == this) Invalidate(_hover.Bounds);
        _hover = null;
        base.OnMouseLeave(e);
    }

    // Selection colours depend on focus.
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing) FallbackEmojiFont.Dispose();
        base.Dispose(disposing);
    }
}
