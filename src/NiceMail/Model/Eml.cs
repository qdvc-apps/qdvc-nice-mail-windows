using System.Globalization;
using System.Text;

namespace Qdvc.NiceMail.Model;

/// <summary>Builds a self-addressed plaintext RFC 5322 message (.eml).</summary>
public static class Eml
{
    public static string Build(string address, string subject, string body, string messageRef, DateTimeOffset date)
    {
        string text = Signature.Normalise(body);
        string fullBody = (text.Length > 0 ? text + "\n\n" : "") + Signature.Trailer(messageRef) + "\n";
        fullBody = Signature.ToCrLf(fullBody);

        bool ascii = fullBody.All(c => c < 128);
        bool longLines = fullBody.Split("\r\n").Any(l => Encoding.UTF8.GetByteCount(l) > 998);

        string domain = address.Contains('@') ? address[(address.LastIndexOf('@') + 1)..] : "localhost";
        var sb = new StringBuilder();
        sb.Append("From: ").Append(address).Append("\r\n");
        sb.Append("To: ").Append(address).Append("\r\n");
        sb.Append("Subject: ").Append(EncodeHeader(subject)).Append("\r\n");
        sb.Append("Date: ").Append(FormatDate(date)).Append("\r\n");
        sb.Append("Message-ID: <").Append(messageRef).Append('.')
          .Append(date.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
          .Append('@').Append(domain).Append(">\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/plain; charset=\"utf-8\"\r\n");

        if (longLines)
        {
            sb.Append("Content-Transfer-Encoding: base64\r\n\r\n");
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullBody));
            for (int i = 0; i < b64.Length; i += 76)
                sb.Append(b64, i, Math.Min(76, b64.Length - i)).Append("\r\n");
        }
        else
        {
            sb.Append("Content-Transfer-Encoding: ").Append(ascii ? "7bit" : "8bit").Append("\r\n\r\n");
            sb.Append(fullBody);
        }
        return sb.ToString();
    }

    /// <summary>RFC 2047 encoded-word for non-ASCII header values.</summary>
    public static string EncodeHeader(string value)
    {
        value = value.Replace("\r", " ").Replace("\n", " ");
        if (value.All(c => c >= 32 && c < 127)) return value;
        return "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "?=";
    }

    /// <summary>RFC 5322 date, e.g. "Mon, 28 Sep 2026 14:03:00 +0100".</summary>
    public static string FormatDate(DateTimeOffset d)
    {
        var off = d.Offset;
        string sign = off < TimeSpan.Zero ? "-" : "+";
        off = off.Duration();
        return d.ToString("ddd, dd MMM yyyy HH:mm:ss ", CultureInfo.InvariantCulture)
             + sign + off.Hours.ToString("00", CultureInfo.InvariantCulture)
             + off.Minutes.ToString("00", CultureInfo.InvariantCulture);
    }

    public static string DefaultFileName(DateTime date, string messageRef) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "-message-ref-" + messageRef + ".eml";
}
