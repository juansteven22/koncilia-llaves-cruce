// Utils/InferidorTipos.cs
using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Ingesta.Utils;

/// <summary>Deduce el tipo SQL de cada columna inspeccionando los primeros N registros.</summary>
public static class InferidorTipos
{
    public static (string Col, string SqlType)[] Inferir(DataTable dt, int muestra = 100)
    {
        int filas = Math.Min(muestra, dt.Rows.Count);
        var resultado = new (string Col, string SqlType)[dt.Columns.Count];

        for (int c = 0; c < dt.Columns.Count; c++)
        {
            var col = dt.Columns[c];
            var valores = dt.AsEnumerable()
                            .Take(filas)
                            .Select(r => r[col]?.ToString() ?? string.Empty)
                            .Where(v => v.Length > 0)
                            .ToList();

            string sql = InferirColumna(valores);
            resultado[c] = (col.ColumnName, sql);
        }
        return resultado;
    }

    /*---------------------------------*/
    /*  Lógica de deducción por columna */
    /*---------------------------------*/
    private static string InferirColumna(System.Collections.Generic.List<string> valores)
    {
        if (valores.Count == 0)
            return "NVARCHAR(MAX)";

        /* INT / BIGINT */
        if (valores.All(v => long.TryParse(v, out _)))
        {
            int maxDig = valores.Max(v => v.Length);
            return maxDig <= 9 ? "INT" :
                   maxDig <= 18 ? "BIGINT" :
                   $"DECIMAL({maxDig},0)";
        }

        /* DECIMAL */
        if (valores.All(v => decimal.TryParse(v,
                        NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                        CultureInfo.InvariantCulture, out _)))
        {
            int maxEnteros = valores.Max(v => v.Split('.', ',')[0].Replace("-", "").Length);
            int maxDecs = valores.Max(v => v.Contains('.') || v.Contains(',')
                                         ? v.Split('.', ',')[1].Length
                                         : 0);
            int precision = maxEnteros + maxDecs;
            return $"DECIMAL({precision},{maxDecs})";
        }

        /* DATETIME (cualquiera que .NET reconozca) */
        if (valores.All(v => DateTime.TryParse(v, CultureInfo.InvariantCulture,
                                               DateTimeStyles.None, out _)))
            return "DATETIME2";

        /* Texto */
        int maxLen = valores.Max(v => v.Length);
        return maxLen <= 4000 ? $"NVARCHAR({maxLen})" : "NVARCHAR(MAX)";
    }
}
