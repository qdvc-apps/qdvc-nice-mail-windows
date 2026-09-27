namespace Qdvc.NiceMail.UI;

/// <summary>A small modal dialog asking for a single- or multi-line value.</summary>
internal sealed class TextPromptDialog : Form
{
    private readonly TextBox _text;

    public string Value => _text.Text;

    public TextPromptDialog(string title, string prompt, string initial, bool multiline = false)
    {
        Text = title;
        FormBorderStyle = multiline ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.None; // sizes below are scaled explicitly
        Padding = new Padding(Ui.Scale(this, 12));

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(multiline ? SizeType.Percent : SizeType.AutoSize, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label { Text = prompt, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
        _text = new TextBox
        {
            Text = initial.Replace("\r\n", "\n").Replace("\n", "\r\n"),
            Dock = DockStyle.Fill,
            Multiline = multiline,
            AcceptsReturn = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
            Font = multiline ? new Font("Segoe UI", 10f) : Font,
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(Ui.Scale(this, 80), 0) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(Ui.Scale(this, 80), 0) };
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(_text, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);

        CancelButton = cancel;
        if (!multiline) AcceptButton = ok; // in multi-line mode Enter inserts a newline; use Ctrl+Enter
        ClientSize = multiline ? new Size(Ui.Scale(this, 520), Ui.Scale(this, 300)) : new Size(Ui.Scale(this, 400), Ui.Scale(this, 130));
        MinimumSize = new Size(Ui.Scale(this, 360), Ui.Scale(this, 170));

        _text.KeyDown += (_, e) =>
        {
            if (multiline && e.Control && e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                e.SuppressKeyPress = true;
            }
        };
        Shown += (_, _) => { _text.Focus(); _text.SelectAll(); };
    }
}
