using System;
using System.Data;
using System.Linq;
using System.Globalization;

namespace Ingesta.Utils;

public static class PostCheckNumericos
{
    /// <summary>Ajusta INT→BIGINT o BIGINT→DECIMAL si algún valor real lo exige.</summary>
    public static (string Col, string SqlType)[] Ajustar(DataTable dt, (string Col, string SqlType)[] tipos)
    {
        return tipos.Select(t =>
        {
            if (t.SqlType == "INT" || t.SqlType.StartsWith("BIGINT"))
            {
                int maxDigits = dt.AsEnumerable()
                                  .Select(r => r[t.Col]?.ToString()?.Trim())
                                  .Where(v => long.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                                  .Select(v => v.Length)
                                  .DefaultIfEmpty(0)
                                  .Max();

                if (t.SqlType == "INT"  && maxDigits > 9)
                    return (t.Col, "BIGINT");

                if (t.SqlType.StartsWith("BIGINT") && maxDigits > 18)
                    return (t.Col, $"DECIMAL({maxDigits},0)");
            }
            return t;
        }).ToArray();
    }
}
