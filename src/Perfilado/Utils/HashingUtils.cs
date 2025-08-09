using System;
using System.Security.Cryptography;
using System.Text;

namespace Perfilado.Utils;

public static class HashingUtils
{
    /* Hash simple SHA-256 para fingerprint de conjuntos */
    public static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
