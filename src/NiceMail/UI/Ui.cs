using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

/// <summary>Glyphs from the Windows icon font (Segoe Fluent Icons / Segoe MDL2 Assets).</summary>
internal static class Glyph
{
    public const char Add = '\uE710';
    public const char Edit = '\uE70F';
    public const char Delete = '\uE74D';
    public const char Copy = '\uE8C8';
    public const char Up = '\uE74A';
    public const char Down = '\uE74B';
    public const char Refresh = '\uE72C';
    public const char Save = '\uE74E';
    public const char Emoji = '\uE76E';
    public const char Tag = '\uE8EC';
}

internal static class Icons
{
    private static readonly Lazy<string?> Family = new(() =>
    {
        using var installed = new InstalledFontCollection();
        var names = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (names.Contains("Segoe Fluent Icons")) return "Segoe Fluent Icons";   // Windows 11
        if (names.Contains("Segoe MDL2 Assets")) return "Segoe MDL2 Assets";     // Windows 10
        return null;
    });

    private static readonly Dictionary<(char, int, int), Bitmap> Cache = new();

    /// <summary>Renders an icon-font glyph to a bitmap; null if no icon font is installed.</summary>
    public static Image? Get(char glyph, int px)
    {
        if (Family.Value is null) return null;
        var color = SystemColors.ControlText;
        var key = (glyph, px, color.ToArgb());
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var bmp = new Bitmap(px, px);
        using (var g = Graphics.FromImage(bmp))
        using (var font = new Font(Family.Value, px * 0.8f, GraphicsUnit.Pixel))
        using (var brush = new SolidBrush(color))
        using (var fmt = new StringFormat(StringFormat.GenericTypographic)
               { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, px, px), fmt);
        }
        Cache[key] = bmp;
        return bmp;
    }
}

internal static class Ui
{
    public static int Scale(Control c, int logical) => (int)Math.Round(logical * c.DeviceDpi / 96.0);

    public static ToolStrip NewToolStrip(Control owner)
    {
        int px = Scale(owner, 16);
        return new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
            Dock = DockStyle.Top,
            ImageScalingSize = new Size(px, px),
            CanOverflow = true,
            Padding = new Padding(Scale(owner, 4), Scale(owner, 2), Scale(owner, 4), Scale(owner, 2)),
        };
    }

    public static ToolStripButton Button(Control owner, string text, char glyph, EventHandler onClick,
                                         string? tooltip = null)
    {
        var b = new ToolStripButton(text, Icons.Get(glyph, Scale(owner, 16)), onClick)
        {
            ToolTipText = tooltip ?? text.Replace("&", ""),
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
        };
        return b;
    }

    public static void ApplyToolbarStyle(ToolStrip strip, ToolbarStyle style)
    {
        foreach (ToolStripItem item in strip.Items)
        {
            if (item is ToolStripButton or ToolStripDropDownButton or ToolStripSplitButton)
            {
                item.DisplayStyle = item.Image is null ? ToolStripItemDisplayStyle.Text : ToolStripItemDisplayStyle.ImageAndText;
                item.TextImageRelation = style == ToolbarStyle.LabelsBelowIcons
                    ? TextImageRelation.ImageAboveText
                    : TextImageRelation.ImageBeforeText;
            }
        }
    }

    /// <summary>
    /// A second toolbar row holding a search box that stretches to the full width.
    /// </summary>
    public static (ToolStrip strip, ToolStripTextBox box) SearchRow(Control owner, EventHandler onChanged)
    {
        var strip = NewToolStrip(owner);
        var label = new ToolStripLabel("&Search:");
        var box = new ToolStripTextBox { AutoSize = false, ToolTipText = "Search (Ctrl+F)" };
        box.TextChanged += onChanged;
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape && box.Text.Length > 0) { box.Clear(); e.SuppressKeyPress = true; }
        };
        strip.Items.Add(label);
        strip.Items.Add(box);
        strip.CanOverflow = false;
        strip.Layout += (_, _) =>
        {
            int w = strip.DisplayRectangle.Width - label.Width - Scale(owner, 12);
            if (w > 50 && box.Width != w) box.Size = new Size(w, box.Height);
        };
        return (strip, box);
    }

    /// <summary>Clipboard writes can fail transiently if another app holds the clipboard open.</summary>
    public static bool TrySetClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(40);
            }
        }
        MessageBox.Show("The clipboard is in use by another application. Please try again.",
            "QDVC Nice Mail", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    /// <summary>The control with keyboard focus, including controls hosted in tool strips.</summary>
    public static Control? FocusedControl()
    {
        var h = GetFocus();
        return h == IntPtr.Zero ? null : Control.FromHandle(h);
    }

    /// <summary>Makes a ListView rows tall enough for emoji by giving it a thin image list.</summary>
    public static void SetRowHeight(ListView list, int px)
    {
        list.SmallImageList?.Dispose();
        list.SmallImageList = new ImageList { ImageSize = new Size(1, px), ColorDepth = ColorDepth.Depth32Bit };
    }

    public static Font DefaultMonoFont() => new("Consolas", 10f);

    public static Font SignatureFont(Preferences prefs)
    {
        if (string.IsNullOrEmpty(prefs.SignatureFontFamily)) return DefaultMonoFont();
        try { return new Font(prefs.SignatureFontFamily, prefs.SignatureFontSize > 0 ? prefs.SignatureFontSize : 10f); }
        catch (ArgumentException) { return DefaultMonoFont(); }
    }
}
