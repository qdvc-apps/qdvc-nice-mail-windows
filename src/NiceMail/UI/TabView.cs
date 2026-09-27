using Qdvc.NiceMail.Model;

namespace Qdvc.NiceMail.UI;

/// <summary>
/// Base class for the content of one tab. Each tab owns its toolbar(s), so the
/// tab strip sits above the toolbar, and the main window's Edit/View commands
/// are routed to whichever tab is active.
/// </summary>
internal abstract class TabView : UserControl
{
    protected TabView(Preferences prefs)
    {
        Prefs = prefs;
        Dock = DockStyle.Fill;
    }

    protected Preferences Prefs { get; }
    protected Workspace? Workspace { get; private set; }

    public event EventHandler<string>? StatusMessage;
    protected void Status(string message) => StatusMessage?.Invoke(this, message);

    public abstract string Title { get; }

    public virtual bool HasSearch => false;
    public virtual void FocusSearch() { }

    public virtual bool HasMessageRef => false;
    public virtual void NewMessageRef() { }

    /// <summary>Edit → Copy for this tab.</summary>
    public abstract void CopyToClipboard();

    /// <summary>View → Refresh: reload from disk (and regenerate message refs where relevant).</summary>
    public abstract void Reload();

    public void SetWorkspace(Workspace? workspace)
    {
        Workspace = workspace;
        Reload();
    }

    public virtual void ApplyPreferences() { }

    protected void ShowError(string action, Exception ex) =>
        MessageBox.Show(this, $"{action} failed:\n\n{ex.Message}", "QDVC Nice Mail",
            MessageBoxButtons.OK, MessageBoxIcon.Error);

    protected bool RequireWorkspace()
    {
        if (Workspace is not null) return true;
        Status("Open a workspace first (File → Open Workspace…).");
        return false;
    }
}
