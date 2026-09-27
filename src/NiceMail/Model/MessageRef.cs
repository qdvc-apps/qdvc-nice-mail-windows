using System.Security.Cryptography;

namespace Qdvc.NiceMail.Model;

public static class MessageRef
{
    /// <summary>Unambiguous alphabet: no 0/O, 1/l/I, 2/Z, 5/S, etc.</summary>
    public const string Alphabet = "346789ABCDEFGHJKLMNPQRTUVWXYabcdefghijkmnpqrtwxyz";
    public const int Length = 10;

    public static string New() =>
        new string(RandomNumberGenerator.GetItems<char>(Alphabet, Length));
}
