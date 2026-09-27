using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

internal sealed class PreferencesDialog : Form
{
    private static readonly (SkinTone tone, string label)[] Tones =
    {
        (SkinTone.None, "Default (yellow) \U0001F44B"),
        (SkinTone.Light, "Light \U0001F44B\U0001F3FB"),
        (SkinTone.MediumLight, "Medium-light \U0001F44B\U0001F3FC"),
        (SkinTone.Medium, "Medium \U0001F44B\U0001F3FD"),
        (SkinTone.MediumDark, "Medium-dark \U0001F44B\U0001F3FE"),
        (SkinTone.Dark, "Dark \U0001F44B\U0001F3FF"),
    };

    private readonly ComboBox _toolbarStyle, _skinTone;
    private readonly Label _fontLabel;
    private readonly CheckBox _reopen;
    private string? _fontFamily;
    private float _fontSize;

    public PreferencesDialog(Preferences prefs)
    {
        Text = "Preferences";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(Ui.Scale(this, 12));

        _fontFamily = prefs.SignatureFontFamily;
        _fontSize = prefs.SignatureFontSize;

        int comboWidth = Ui.Scale(this, 220);
        var grid = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Label Caption(string t) => new()
        {
            Text = t, AutoSize = true, Anchor = AnchorStyles.Left,
            Margin = new Padding(0, Ui.Scale(this, 6), Ui.Scale(this, 12), Ui.Scale(this, 6)),
        };

        _toolbarStyle = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = comboWidth, Anchor = AnchorStyles.Left };
        _toolbarStyle.Items.AddRange(new object[] { "Labels beside icons", "Labels below icons" });
        _toolbarStyle.SelectedIndex = prefs.ToolbarStyle == ToolbarStyle.LabelsBelowIcons ? 1 : 0;

        _skinTone = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = comboWidth, Anchor = AnchorStyles.Left };
        _skinTone.Items.AddRange(Tones.Select(t => (object)t.label).ToArray());
        _skinTone.SelectedIndex = Math.Max(0, Array.FindIndex(Tones, t => t.tone == prefs.SkinTone));

        _fontLabel = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, Ui.Scale(this, 6), Ui.Scale(this, 8), 0) };
        var choose = new Button { Text = "&Choose…", AutoSize = true };
        choose.Click += (_, _) => ChooseFont();
        var reset = new Button { Text = "&Default", AutoSize = true };
        reset.Click += (_, _) => { _fontFamily = null; _fontSize = 10f; UpdateFontLabel(); };
        var fontRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        fontRow.Controls.AddRange(new Control[] { _fontLabel, choose, reset });

        _reopen = new CheckBox { Text = "&Reopen the last workspace on startup", AutoSize = true, Checked = prefs.ReopenLastWorkspace,
                                 Margin = new Padding(0, Ui.Scale(this, 10), 0, 0) };

        grid.Controls.Add(Caption("&Toolbar style:"), 0, 0);
        grid.Controls.Add(_toolbarStyle, 1, 0);
        grid.Controls.Add(Caption("Emoji &skin tone:"), 0, 1);
        grid.Controls.Add(_skinTone, 1, 1);
        grid.Controls.Add(Caption("Signature &font:"), 0, 2);
        grid.Controls.Add(fontRow, 1, 2);
        grid.Controls.Add(_reopen, 0, 3);
        grid.SetColumnSpan(_reopen, 2);

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, MinimumSize = new Size(Ui.Scale(this, 80), 0), AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, MinimumSize = new Size(Ui.Scale(this, 80), 0), AutoSize = true };
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill,
                                            Margin = new Padding(0, Ui.Scale(this, 16), 0, 0) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        grid.Controls.Add(buttons, 0, 4);
        grid.SetColumnSpan(buttons, 2);

        Controls.Add(grid);
        AcceptButton = ok;
        CancelButton = cancel;
        UpdateFontLabel();
    }

    private void UpdateFontLabel() =>
        _fontLabel.Text = _fontFamily is null ? "Default (Consolas 10)" : $"{_fontFamily} {_fontSize:0.#}";

    private void ChooseFont()
    {
        using var current = _fontFamily is null ? Ui.DefaultMonoFont() : new Font(_fontFamily, _fontSize);
        using var dlg = new FontDialog { Font = current, ShowEffects = false, AllowVerticalFonts = false };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _fontFamily = dlg.Font.FontFamily.Name;
        _fontSize = dlg.Font.SizeInPoints;
        UpdateFontLabel();
    }

    public void ApplyTo(Preferences prefs)
    {
        prefs.ToolbarStyle = _toolbarStyle.SelectedIndex == 1 ? ToolbarStyle.LabelsBelowIcons : ToolbarStyle.LabelsBesideIcons;
        prefs.SkinTone = Tones[Math.Max(0, _skinTone.SelectedIndex)].tone;
        prefs.SignatureFontFamily = _fontFamily;
        prefs.SignatureFontSize = _fontSize;
        prefs.ReopenLastWorkspace = _reopen.Checked;
    }
}
