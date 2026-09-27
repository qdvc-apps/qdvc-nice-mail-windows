using Qdvc.NiceMail.Model;
using Qdvc.NiceMail.UI;

namespace Qdvc.NiceMail;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var prefs = Preferences.Load();

        // Must be set before any window is created. Dark mode needs Windows 11;
        // on older versions WinForms falls back to the light (classic) scheme.
        Application.SetColorMode(prefs.Theme switch
        {
            AppTheme.Light => SystemColorMode.Classic,
            AppTheme.Dark => SystemColorMode.Dark,
            _ => SystemColorMode.System,
        });

        string? workspace = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (workspace is null && prefs.ReopenLastWorkspace && !string.IsNullOrEmpty(prefs.LastWorkspace)
            && Directory.Exists(prefs.LastWorkspace))
        {
            workspace = prefs.LastWorkspace;
        }

        Application.Run(new MainForm(prefs, workspace));
        ColorEmoji.Shutdown();
    }
}
