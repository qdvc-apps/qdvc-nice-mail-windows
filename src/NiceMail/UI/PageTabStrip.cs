using System.ComponentModel;

namespace Qdvc.NiceMail.UI;

/// <summary>
/// A tab strip built on ToolStrip, replacing TabControl, whose themed tab
/// headers have very low contrast on Windows 11 in both light and dark mode.
/// The selected tab is bold with an accent underline (the Windows 11 "pivot"
/// pattern). All colours come from SystemColors, so it follows light mode,
/// dark mode and high contrast.
///
/// The strip only chooses a page; the owner shows and hides page content and
/// handles Ctrl+Tab / Ctrl+PgUp / Ctrl+PgDn (see MainForm).
/// </summary>
internal sealed class PageTabStrip : ToolStrip
{
    private readonly Font _regularFont;
    private readonly Font _boldFont;
    private int _selectedIndex = -1;

    public event EventHandler? SelectedIndexChanged;

    public PageTabStrip()
    {
        GripStyle = ToolStripGripStyle.Hidden;
        Dock = DockStyle.Top;
        CanOverflow = false;
        Stretch = true;
        AccessibleRole = AccessibleRole.PageTabList;
        AccessibleName = "Sections";
        Padding = new Padding(Ui.Scale(this, 8), Ui.Scale(this, 2), Ui.Scale(this, 8), 0);
        Renderer = new PageTabRenderer();

        _regularFont = new Font(Font, FontStyle.Regular);
        _boldFont = new Font(Font, FontStyle.Bold);
    }

    public int TabCount => Items.Count;

    public void AddTab(string text)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            AccessibleRole = AccessibleRole.PageTab,
            AccessibleName = text.Replace("&", ""),
            AutoSize = false,
            Font = _regularFont,
            Margin = Padding.Empty,
            AutoToolTip = false,
        };
        // Fixed width measured in bold, so tabs don't shift when selection changes.
        int width = TextRenderer.MeasureText(text, _boldFont, Size.Empty, TextFormatFlags.SingleLine).Width
                    + Ui.Scale(this, 28);
        button.Size = new Size(width, Ui.Scale(this, 36));
        button.Click += (_, _) => SelectedIndex = Items.IndexOf(button);
        Items.Add(button);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= Items.Count || value == _selectedIndex) return;
            _selectedIndex = value;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is not ToolStripButton b) continue;
                b.Checked = i == value;
                b.Font = i == value ? _boldFont : _regularFont;
            }
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Moves the selection by <paramref name="delta"/>, wrapping around.</summary>
    public void Cycle(int delta)
    {
        if (Items.Count == 0) return;
        int next = ((_selectedIndex + delta) % Items.Count + Items.Count) % Items.Count;
        SelectedIndex = next;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _regularFont.Dispose();
            _boldFont.Dispose();
        }
    }

    private sealed class PageTabRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(SystemColors.Control);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // A single divider under the strip, separating it from the page's own toolbar.
            var r = e.ToolStrip.ClientRectangle;
            using var p = new Pen(Ui.Blend(SystemColors.Control, SystemColors.ControlText, 0.18));
            e.Graphics.DrawLine(p, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is not ToolStripButton button) return;
            var r = new Rectangle(Point.Empty, button.Size);

            double tint = button.Pressed ? 0.14 : button.Selected ? 0.07 : 0; // Selected == hover
            if (tint > 0)
            {
                using var b = new SolidBrush(Ui.Blend(SystemColors.Control, SystemColors.ControlText, tint));
                e.Graphics.FillRectangle(b, r);
            }

            if (button.Checked)
            {
                int h = Ui.Scale(e.ToolStrip!, 3);
                int inset = Ui.Scale(e.ToolStrip!, 10);
                using var accent = new SolidBrush(SystemColors.Highlight);
                e.Graphics.FillRectangle(accent, r.Left + inset, r.Bottom - h - 1, r.Width - 2 * inset, h);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            bool selected = e.Item is ToolStripButton { Checked: true };
            e.TextColor = selected
                ? SystemColors.ControlText
                : Ui.Blend(SystemColors.ControlText, SystemColors.Control, 0.22);
            base.OnRenderItemText(e);
        }
    }
}
