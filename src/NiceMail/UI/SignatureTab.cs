using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

internal sealed class SignatureTab : TabView
{
    private readonly ToolStrip _toolbar;
    private readonly ToolStripComboBox _profile;
    private readonly ToolStripButton _disclaimer, _refOnly, _newRef, _copy;
    private readonly TextBox _preview;

    private string _signoff = "", _disclaimerText = "", _profileText = "";
    private string _messageRef = MessageRef.New();
    private bool _loading;
    private Font? _ownedFont;

    public SignatureTab(Preferences prefs) : base(prefs)
    {
        _toolbar = Ui.NewToolStrip(this);
        _profile = new ToolStripComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            ToolTipText = "Signature profile",
            AutoSize = false,
            Width = Ui.Scale(this, 160),
        };
        _profile.SelectedIndexChanged += (_, _) => OnProfileChanged();
        _disclaimer = new ToolStripButton("Disclaimer") { CheckOnClick = true, ToolTipText = "Include the disclaimer" };
        _disclaimer.CheckedChanged += (_, _) => { if (!_loading) { Prefs.IncludeDisclaimer = _disclaimer.Checked; Render(); } };
        _refOnly = new ToolStripButton("Ref Only") { CheckOnClick = true, ToolTipText = "Only the m-dash and message ref" };
        _refOnly.CheckedChanged += (_, _) => { if (!_loading) { Prefs.RefOnly = _refOnly.Checked; Render(); } };
        _newRef = Ui.Button(this, "New Ref", Glyph.Refresh, (_, _) => NewMessageRef(), "Generate a new message ref (F5)");
        _copy = Ui.Button(this, "Copy", Glyph.Copy, (_, _) => CopyToClipboard(), "Copy the signature (Ctrl+C)");
        _toolbar.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripLabel("&Profile:"), _profile, new ToolStripSeparator(),
            _disclaimer, _refOnly, new ToolStripSeparator(), _newRef, new ToolStripSeparator(), _copy,
        });

        _preview = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.None,
        };
        var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Ui.Scale(this, 12)), BackColor = SystemColors.Window };
        frame.Controls.Add(_preview);

        Controls.Add(frame);
        Controls.Add(_toolbar);

        _loading = true;
        _disclaimer.Checked = Prefs.IncludeDisclaimer;
        _refOnly.Checked = Prefs.RefOnly;
        _loading = false;

        ApplyPreferences();
    }

    public override string Title => "Signature";
    public override bool HasMessageRef => true;

    public override void ApplyPreferences()
    {
        Ui.ApplyToolbarStyle(_toolbar, Prefs.ToolbarStyle);
        var old = _ownedFont;
        _ownedFont = Ui.SignatureFont(Prefs);
        _preview.Font = _ownedFont;
        old?.Dispose();
    }

    public override void NewMessageRef()
    {
        _messageRef = MessageRef.New();
        Render();
        Status($"New message ref: {_messageRef}");
    }

    public override void Reload()
    {
        _loading = true;
        try
        {
            _signoff = _disclaimerText = _profileText = "";
            _profile.Items.Clear();
            if (Workspace is not null)
            {
                try
                {
                    _signoff = Workspace.ReadSignoff();
                    _disclaimerText = Workspace.ReadDisclaimer();
                    foreach (var name in Workspace.ListProfiles()) _profile.Items.Add(name);
                }
                catch (IOException ex) { ShowError("Loading the signature", ex); }
            }
            int idx = Prefs.Profile is null ? -1 : _profile.Items.IndexOf(Prefs.Profile);
            if (idx < 0 && _profile.Items.Count > 0) idx = 0;
            _profile.SelectedIndex = idx;
            _profileText = LoadProfile();
        }
        finally
        {
            _loading = false;
        }
        _messageRef = MessageRef.New();
        Render();
    }

    private string LoadProfile()
    {
        if (Workspace is null || _profile.SelectedItem is not string name) return "";
        try { return Workspace.ReadProfile(name); }
        catch (IOException ex) { ShowError("Loading the profile", ex); return ""; }
    }

    private void OnProfileChanged()
    {
        if (_loading) return;
        Prefs.Profile = _profile.SelectedItem as string;
        _profileText = LoadProfile();
        Render();
    }

    private string Current() =>
        Signature.Build(_signoff, _profileText, _disclaimerText, _disclaimer.Checked, _refOnly.Checked, _messageRef);

    private void Render()
    {
        _disclaimer.Enabled = !_refOnly.Checked;
        _profile.Enabled = !_refOnly.Checked && _profile.Items.Count > 0;
        _preview.Text = Workspace is null && !_refOnly.Checked
            ? "Open a workspace (File → Open Workspace…) to assemble your signature."
            : Signature.ToCrLf(Current());
    }

    public override void CopyToClipboard()
    {
        if (Workspace is null && !_refOnly.Checked) { RequireWorkspace(); return; }
        if (Ui.TrySetClipboard(Signature.ToCrLf(Current()))) Status("Copied signature");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _ownedFont?.Dispose();
    }
}
