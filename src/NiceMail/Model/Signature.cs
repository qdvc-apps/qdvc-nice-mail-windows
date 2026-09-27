namespace Qdvc.NiceMail.Model;

public static class Signature
{
    public const string MDash = "\u2014";

    /// <summary>
    /// Assembles the plaintext signature using "\n" line endings.
    /// Layout: signoff, blank line, m-dash, blank line, profile,
    /// [blank line, disclaimer], blank line, "Message ref. XXXXXXXXXX".
    /// </summary>
    public static string Build(string signoff, string profile, string? disclaimer,
                               bool includeDisclaimer, bool refOnly, string messageRef)
    {
        string refLine = "Message ref. " + messageRef;
        if (refOnly)
            return MDash + "\n\n" + refLine;

        var parts = new List<string>();
        string so = Normalise(signoff);
        if (so.Length > 0) parts.Add(so);   // followed by a blank line, then the m-dash
        parts.Add(MDash);
        string pr = Normalise(profile);
        if (pr.Length > 0) parts.Add(pr);
        if (includeDisclaimer)
        {
            string di = Normalise(disclaimer ?? "");
            if (di.Length > 0) parts.Add(di);
        }
        parts.Add(refLine);
        return string.Join("\n\n", parts);
    }

    /// <summary>The trailer appended to a Note to Self body.</summary>
    public static string Trailer(string messageRef) => MDash + "\n\nMessage ref. " + messageRef;

    internal static string Normalise(string s) =>
        s.Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n', ' ', '\t');

    public static string ToCrLf(string s) => s.Replace("\r\n", "\n").Replace("\n", "\r\n");
}
