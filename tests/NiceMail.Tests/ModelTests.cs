using Qdvc.NiceMail.Model;
using Xunit;

namespace Qdvc.NiceMail.Tests;

public class CsvTests
{
    [Fact]
    public void RoundTripsQuotesCommasAndNewlines()
    {
        var rows = new[] { new[] { "id", "text" }, new[] { "1", "Hello, \"world\"\nline two" } };
        var parsed = Csv.Parse(Csv.Format(rows));
        Assert.Equal(2, parsed.Count);
        Assert.Equal("Hello, \"world\"\nline two", parsed[1][1]);
    }

    [Fact]
    public void ParsesHeaderAndMissingColumns()
    {
        var rows = Csv.ParseWithHeader("id,label,char\r\nwaving_hand_sign\r\n");
        Assert.Single(rows);
        Assert.Equal("waving_hand_sign", rows[0]["id"]);
        Assert.Equal("", rows[0]["label"]);
    }
}

public class SignatureTests
{
    [Fact]
    public void BuildsFullSignature()
    {
        string sig = Signature.Build("Kind regards,\n\nJohn Smith\n", "John Smith\nSuperhero\n",
            "Disclaimer: text\n", includeDisclaimer: true, refOnly: false, "YyM4mRnjHQ");
        Assert.Equal(
            "Kind regards,\n\nJohn Smith\n\n\u2014\n\nJohn Smith\nSuperhero\n\nDisclaimer: text\n\nMessage ref. YyM4mRnjHQ",
            sig);
    }

    [Fact]
    public void OmitsDisclaimerWhenToggledOff()
    {
        string sig = Signature.Build("Bye", "Me", "Disclaimer", includeDisclaimer: false, refOnly: false, "ABCDEFGHJK");
        Assert.DoesNotContain("Disclaimer", sig);
    }

    [Fact]
    public void RefOnlyKeepsJustDashAndRef()
    {
        string sig = Signature.Build("Bye", "Me", "Disclaimer", includeDisclaimer: true, refOnly: true, "ABCDEFGHJK");
        Assert.Equal("\u2014\n\nMessage ref. ABCDEFGHJK", sig);
    }
}

public class MessageRefTests
{
    [Fact]
    public void UsesUnambiguousAlphabet()
    {
        for (int i = 0; i < 200; i++)
        {
            string r = MessageRef.New();
            Assert.Equal(10, r.Length);
            Assert.All(r, c => Assert.True(MessageRef.Alphabet.Contains(c), $"unexpected {c}"));
        }
    }
}

public class EmojiTests
{
    private static readonly EmojiCatalogue Catalogue = EmojiCatalogue.LoadEmbedded();

    [Fact]
    public void CatalogueUsesSnakeCaseUnicodeNames()
    {
        Assert.NotNull(Catalogue.Find("waving_hand_sign"));
        Assert.NotNull(Catalogue.Find(Workspace.DefaultFavouriteId));
    }

    [Fact]
    public void AppliesSkinToneOnlyToModifierBases()
    {
        var hand = Catalogue.Find("waving_hand_sign")!;
        Assert.Equal("\U0001F44B\U0001F3FD", hand.WithTone(SkinTone.Medium));
        var smile = Catalogue.Find(Workspace.DefaultFavouriteId)!;
        Assert.Equal(smile.Char, smile.WithTone(SkinTone.Dark));
    }

    [Fact]
    public void FindsPastedGlyphIgnoringToneAndVariationSelector()
    {
        Assert.Equal("waving_hand_sign", Catalogue.FindByChar("\U0001F44B\U0001F3FF")?.Id);
        Assert.Equal("heavy_black_heart", Catalogue.FindByChar("\u2764")?.Id);
    }

    [Fact]
    public void CustomIdIsLowercaseCodepoints()
    {
        Assert.Equal("custom_2764_fe0f_200d_1fa79", EmojiCatalogue.CustomId("\u2764\uFE0F\u200D\U0001FA79"));
    }
}

public class EmlTests
{
    [Fact]
    public void BuildsSelfAddressedMessageWithTrailer()
    {
        var date = new DateTimeOffset(2026, 9, 28, 14, 3, 0, TimeSpan.FromHours(1));
        string eml = Eml.Build("me@example.com", "Hello", "Body text", "ABCDEFGHJK", date);
        Assert.Contains("From: me@example.com\r\n", eml);
        Assert.Contains("To: me@example.com\r\n", eml);
        Assert.Contains("Date: Mon, 28 Sep 2026 14:03:00 +0100\r\n", eml);
        Assert.EndsWith("Body text\r\n\r\n\u2014\r\n\r\nMessage ref. ABCDEFGHJK\r\n", eml);
    }

    [Fact]
    public void EncodesNonAsciiSubject()
    {
        Assert.StartsWith("=?utf-8?B?", Eml.EncodeHeader("Café ☕"));
    }

    [Fact]
    public void DefaultFileNameMatchesSpec()
    {
        Assert.Equal("2026-09-28-message-ref-ABCDEFGHJK.eml", Eml.DefaultFileName(new DateTime(2026, 9, 28), "ABCDEFGHJK"));
    }
}

public class WorkspaceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "nicemail-test-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void EnsureLayoutCreatesDefaultsAndRoundTrips()
    {
        var ws = new Workspace(_dir);
        ws.EnsureLayout();
        Assert.Equal(Workspace.DefaultFavouriteId, Assert.Single(ws.LoadFavourites()).Id);
        Assert.Contains("default", ws.ListProfiles());

        ws.SavePhrases(new[] { new Phrase { Id = "1", Text = "Thanks, \"all\"" } });
        Assert.Equal("Thanks, \"all\"", Assert.Single(ws.LoadPhrases()).Text);
        Assert.Equal("2", Workspace.NextPhraseId(ws.LoadPhrases()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}
