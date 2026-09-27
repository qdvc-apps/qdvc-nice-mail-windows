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
        string? workspace = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (workspace is null && prefs.ReopenLastWorkspace && !string.IsNullOrEmpty(prefs.LastWorkspace)
            && Directory.Exists(prefs.LastWorkspace))
        {
            workspace = prefs.LastWorkspace;
        }

        Application.Run(new MainForm(prefs, workspace));
    }
}
