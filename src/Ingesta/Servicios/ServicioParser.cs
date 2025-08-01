using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Ingesta.Extensiones;
using Ingesta.Modelos;

public class ServicioParser
{
    // Devuelvo un DataTable listo para SqlBulkCopy
    public DataTable Parsear(string rutaArchivo, EsquemaArchivo schema)
    {
        return schema.type.ToLower() switch
        {
            "fixed" => ParsearFixed(rutaArchivo, schema),
            "csv"   => ParsearCsv(rutaArchivo, schema),
            _       => throw new NotSupportedException($"Tipo {schema.type} no soportado")
        };
    }

    private DataTable ParsearFixed(string ruta, EsquemaArchivo s)
    {
        var dt = CrearDataTable(s);
        foreach (var linea in File.ReadLines(ruta))
        {
            foreach (var seccion in s.Sections)
            {
                if (seccion.Filter is null || Regex.IsMatch(linea, seccion.Filter))
                {
                    var fila = dt.NewRow();
                    foreach (var campo in seccion.Fields)
                    {
                        var valor = linea.SafeSubstring(campo.fOffset, campo.fLength);
                        fila[campo.fName] = valor.Trim();
                    }
                    dt.Rows.Add(fila);
                }
            }
        }
        return dt;
    }

    private DataTable ParsearCsv(string ruta, EsquemaArchivo s)
    {
        var dt = CrearDataTable(s);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = UnescapeSeparator(s.separator ?? ","),
            HasHeaderRecord = s.header
        };
        using var reader = new StreamReader(ruta);
        using var csv    = new CsvReader(reader, cfg);
        while (csv.Read())
        {
            var fila = dt.NewRow();
            foreach (var campo in s.Sections[0].Fields) // csv suele tener 1 sección
            {
                fila[campo.fName] = csv.GetField(campo.fName) ?? string.Empty;
            }
            dt.Rows.Add(fila);
        }
        return dt;
    }

    private static DataTable CrearDataTable(EsquemaArchivo s)
    {
        var dt = new DataTable();
        foreach (var campo in s.Sections.SelectMany(sec => sec.Fields))
            dt.Columns.Add(campo.fName, typeof(string)); // simple: todo texto
        return dt;
    }

    private static string UnescapeSeparator(string raw) =>
        raw.Replace("\\t","\t").Replace("\\&","&");
}
