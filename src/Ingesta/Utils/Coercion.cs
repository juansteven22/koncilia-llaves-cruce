// Utils/Coercion.cs
using System;
using System.Data;
using System.Globalization;

namespace Ingesta.Utils;

public static class Coercion
{
    /// <summary>
    /// Convierte valores de DataTable a su tipo SQL.
    /// "" o tokens como "NA" → NULL.
    /// </summary>
    public static void Convertir(DataTable dt, (string Col, string SqlType)[] colsTipos)
    {
        foreach (var (col, sql) in colsTipos)
        {
            if (!dt.Columns.Contains(col)) continue;

            bool EsNulo(string? v) =>
                string.IsNullOrWhiteSpace(v) ||
                v.Equals("NA",   StringComparison.OrdinalIgnoreCase) ||
                v.Equals("N/A",  StringComparison.OrdinalIgnoreCase) ||
                v.Equals("NULL", StringComparison.OrdinalIgnoreCase);

            /*────────────────────── BIGINT / INT ──────────────────────*/
            if (sql.StartsWith("BIGINT") || sql == "INT")
            {
                foreach (DataRow r in dt.Rows)
                {
                    var v = r[col]?.ToString();
                    r[col] = EsNulo(v) ? DBNull.Value
                           : long.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n)
                                 ? n
                                 : DBNull.Value;   // si falla parseo → NULL
                }
            }
            /*────────────────────── DECIMAL(p,s) ──────────────────────*/
            else if (sql.StartsWith("DECIMAL"))
            {
                foreach (DataRow r in dt.Rows)
                {
                    var v = r[col]?.ToString();
                    r[col] = EsNulo(v) ? DBNull.Value
                           : decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                                 ? d
                                 : DBNull.Value;
                }
            }
            /*────────────────────── DATETIME2 ────────────────────────*/
            else if (sql == "DATETIME2")
            {
                foreach (DataRow r in dt.Rows)
                {
                    var v = r[col]?.ToString();
                    r[col] = EsNulo(v) ? DBNull.Value
                           : DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtm)
                                 ? dtm
                                 : DBNull.Value;
                }
            }
            /* NVARCHAR … no requiere conversión */
        }
    }
}
