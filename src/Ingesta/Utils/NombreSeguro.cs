// Utils/NombreSeguro.cs
using System.Text.RegularExpressions;

namespace Ingesta.Utils;

public static class NombreSeguro
{
    private static readonly Regex _noValido = new(@"[^\w]", RegexOptions.Compiled);

    public static string Limpiar(string texto) =>
        _noValido.Replace(texto, "_");   // Sustituyo puntos, guiones, espacios…
}
