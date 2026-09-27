using System.Globalization;
using System.Text;

namespace Qdvc.NiceMail.Model;

public sealed class Favourite
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>Glyph for custom (pasted) emoji; blank for catalogue emoji.</summary>
    public string Char { get; set; } = "";
    public bool IsCustom => Id.StartsWith("custom_", StringComparison.Ordinal);
}

public sealed class Phrase
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// A plain-text workspace folder. Layout is identical to the GTK app so the
/// same folder can be shared between both front-ends.
/// </summary>
public sealed class Workspace
{
    public const string FavouritesFile = "favourite_emoji.csv";
    public const string PhrasesFile = "phrases.csv";
    public const string DefaultFavouriteId = "smiling_face_with_smiling_eyes";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public string Root { get; }
    public string MailSigsDir => Path.Combine(Root, "mailsigs");
    public string ProfilesDir => Path.Combine(MailSigsDir, "profiles");
    public string SignoffPath => Path.Combine(MailSigsDir, "signoff.txt");
    public string DisclaimerPath => Path.Combine(MailSigsDir, "disclaimer.txt");

    public Workspace(string root)
    {
        Root = Path.GetFullPath(root);
    }

    /// <summary>Creates any missing files and folders with sensible defaults.</summary>
    public void EnsureLayout()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(ProfilesDir);

        CreateIfMissing(Path.Combine(Root, FavouritesFile),
            Csv.Format(new[] { new[] { "id", "label", "char" }, new[] { DefaultFavouriteId, "", "" } }));
        CreateIfMissing(Path.Combine(Root, PhrasesFile),
            Csv.Format(new[] { new[] { "id", "text" } }));
        CreateIfMissing(SignoffPath, "Kind regards,\n\nYour Name\n");
        CreateIfMissing(DisclaimerPath, "Disclaimer: a disclaimer text goes here\n");
        if (!Directory.EnumerateFiles(ProfilesDir, "*.txt").Any())
            CreateIfMissing(Path.Combine(ProfilesDir, "default.txt"), "Your Name\nYour Role\n");
    }

    private static void CreateIfMissing(string path, string content)
    {
        if (!File.Exists(path)) File.WriteAllText(path, content, Utf8NoBom);
    }

    private static void WriteAtomically(string path, string content)
    {
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content, Utf8NoBom);
        File.Move(tmp, path, overwrite: true);
    }

    private static string ReadText(string path) =>
        File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "";

    // ---- Favourites ----------------------------------------------------

    public List<Favourite> LoadFavourites()
    {
        var list = new List<Favourite>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in Csv.ParseWithHeader(ReadText(Path.Combine(Root, FavouritesFile))))
        {
            string id = row.GetValueOrDefault("id", "").Trim();
            if (id.Length == 0 || !seen.Add(id)) continue;
            list.Add(new Favourite
            {
                Id = id,
                Label = row.GetValueOrDefault("label", ""),
                Char = row.GetValueOrDefault("char", ""),
            });
        }
        return list;
    }

    public void SaveFavourites(IEnumerable<Favourite> favourites)
    {
        var rows = new List<IEnumerable<string>> { new[] { "id", "label", "char" } };
        rows.AddRange(favourites.Select(f => new[] { f.Id, f.Label, f.IsCustom ? f.Char : "" }));
        WriteAtomically(Path.Combine(Root, FavouritesFile), Csv.Format(rows));
    }

    // ---- Phrases -------------------------------------------------------

    public List<Phrase> LoadPhrases()
    {
        return Csv.ParseWithHeader(ReadText(Path.Combine(Root, PhrasesFile)))
            .Select(r => new Phrase { Id = r.GetValueOrDefault("id", ""), Text = r.GetValueOrDefault("text", "") })
            .Where(p => p.Text.Length > 0)
            .ToList();
    }

    public void SavePhrases(IEnumerable<Phrase> phrases)
    {
        var rows = new List<IEnumerable<string>> { new[] { "id", "text" } };
        rows.AddRange(phrases.Select(p => new[] { p.Id, p.Text }));
        WriteAtomically(Path.Combine(Root, PhrasesFile), Csv.Format(rows));
    }

    /// <summary>Next free id: one more than the largest numeric id in use.</summary>
    public static string NextPhraseId(IEnumerable<Phrase> phrases)
    {
        int max = 0;
        foreach (var p in phrases)
            if (int.TryParse(p.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > max)
                max = n;
        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }

    // ---- Signature -----------------------------------------------------

    public string ReadSignoff() => ReadText(SignoffPath);
    public string ReadDisclaimer() => ReadText(DisclaimerPath);

    public List<string> ListProfiles()
    {
        if (!Directory.Exists(ProfilesDir)) return new List<string>();
        return Directory.EnumerateFiles(ProfilesDir, "*.txt")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OfType<string>()
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public string ReadProfile(string name) => ReadText(Path.Combine(ProfilesDir, name + ".txt"));
}
