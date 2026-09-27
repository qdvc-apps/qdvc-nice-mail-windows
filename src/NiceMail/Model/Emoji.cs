using System.Globalization;
using System.Reflection;
using System.Text;

namespace Qdvc.NiceMail.Model;

public enum SkinTone { None, Light, MediumLight, Medium, MediumDark, Dark }

public sealed class EmojiInfo
{
    public required string Id { get; init; }
    /// <summary>Base glyph (may include U+FE0F), without any skin tone.</summary>
    public required string Char { get; init; }
    public required string Name { get; init; }
    public bool ModifierBase { get; init; }
    public bool IsCustom { get; init; }

    public string WithTone(SkinTone tone)
    {
        if (!ModifierBase || tone == SkinTone.None) return Char;
        string bare = Char.Replace("\uFE0F", "");
        return bare + char.ConvertFromUtf32(0x1F3FB + (int)tone - 1);
    }
}

public sealed class EmojiCatalogue
{
    private readonly Dictionary<string, EmojiInfo> _byId;
    private readonly Dictionary<string, EmojiInfo> _byChar;

    public IReadOnlyList<EmojiInfo> All { get; }

    public EmojiCatalogue(IEnumerable<EmojiInfo> items)
    {
        All = items.ToList();
        _byId = new Dictionary<string, EmojiInfo>(StringComparer.Ordinal);
        _byChar = new Dictionary<string, EmojiInfo>(StringComparer.Ordinal);
        foreach (var e in All)
        {
            _byId.TryAdd(e.Id, e);
            _byChar.TryAdd(e.Char.Replace("\uFE0F", ""), e);
        }
    }

    public EmojiInfo? Find(string id) => _byId.TryGetValue(id, out var e) ? e : null;

    /// <summary>Finds a catalogue entry for a pasted glyph, ignoring VS16 and skin tones.</summary>
    public EmojiInfo? FindByChar(string glyph)
    {
        var sb = new StringBuilder();
        foreach (var rune in glyph.EnumerateRunes())
        {
            if (rune.Value == 0xFE0F || (rune.Value >= 0x1F3FB && rune.Value <= 0x1F3FF)) continue;
            sb.Append(rune.ToString());
        }
        return _byChar.TryGetValue(sb.ToString(), out var e) ? e : null;
    }

    public static EmojiCatalogue LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Qdvc.NiceMail.emoji.tsv")
            ?? throw new InvalidOperationException("Embedded emoji catalogue is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Parse(reader.ReadToEnd());
    }

    public static EmojiCatalogue Parse(string tsv)
    {
        var items = new List<EmojiInfo>();
        foreach (var line in tsv.Split('\n'))
        {
            var cols = line.TrimEnd('\r').Split('\t');
            if (cols.Length < 4) continue;
            var sb = new StringBuilder();
            foreach (var hex in cols[1].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                sb.Append(char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
            items.Add(new EmojiInfo
            {
                Id = cols[0],
                Char = sb.ToString(),
                Name = cols[2],
                ModifierBase = cols[3] == "1",
            });
        }
        return new EmojiCatalogue(items);
    }

    /// <summary>Stable id for a pasted glyph: custom_&lt;codepoints&gt;, lowercase hex.</summary>
    public static string CustomId(string glyph) =>
        "custom_" + string.Join("_", glyph.EnumerateRunes().Select(r => r.Value.ToString("x", CultureInfo.InvariantCulture)));
}
