// Utils/Normalizacion.cs
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Etiquetado.Utils;

public static class Normalizacion
{
    // Normalizo el nombre de tabla
    public static string Tabla(string t) =>
        t?.Trim().ToUpperInvariant() ?? string.Empty;

    // Normalizo y ordeno las columnas para evitar duplicados por orden distinto
    public static string ColumnasCsv(string[] cols)
    {
        var norm = cols
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .OrderBy(c => c)
            .ToArray();
        return string.Join(",", norm);
    }

    // Hash SHA-256 sobre la expresión canónica "TA|CA=>TB|CB"
    public static string HashEtiqueta(string tablaA, string colsA, string tablaB, string colsB)
    {
        var canonical = $"{tablaA}|{colsA}=>{tablaB}|{colsB}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes); // 64 chars hex
    }
}
