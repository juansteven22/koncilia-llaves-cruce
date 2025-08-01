namespace Ingesta.Extensiones;
public static class StringExtensions
{
    // Evito excepción al cortar cadenas cortas
    public static string SafeSubstring(this string s, int offset, int length)
        => offset >= s.Length ? string.Empty
        :   s.Substring(offset, Math.Min(length, s.Length - offset));
}
