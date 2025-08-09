// ServicioParser.cs
// Lee archivos FIXED y CSV aplicando filtros y devolviendo un DataTable.

using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Ingesta.Extensiones;
using Ingesta.Modelos;

namespace Ingesta.Servicios;

public class ServicioParser
{
    public DataTable Parsear(string rutaArchivo, EsquemaArchivo schema)
    {
        return schema.type.ToLower() switch
        {
            "fixed" or "fixedexact" => ParsearFixed(rutaArchivo, schema),
            "csv"                   => ParsearCsv(rutaArchivo, schema),
            _ => throw new NotSupportedException($"Tipo {schema.type} no soportado")
        };
    }

    /*───────────────────────────  FIXED  ───────────────────────────*/
    private DataTable ParsearFixed(string ruta, EsquemaArchivo s)
    {
        var dt = CrearDataTable(s);

        foreach (var linea in File.ReadLines(ruta))
        {
            foreach (var sec in s.Sections)
            {
                if (sec.Filter != null && !Regex.IsMatch(linea, sec.Filter))
                    continue;

                var fila = dt.NewRow();
                foreach (var campo in sec.Fields)
                {
                    string valor = linea.SafeSubstring(campo.fOffset, campo.fLength).Trim();
                    fila[campo.fName] = valor;
                }
                dt.Rows.Add(fila);
            }
        }
        return dt;
    }

    /*───────────────────────────  CSV  ────────────────────────────*/
    private DataTable ParsearCsv(string ruta, EsquemaArchivo s)
    {
        var dt = CrearDataTable(s);

        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter          = UnescapeSeparator(s.separator ?? ","),
            HasHeaderRecord    = s.header,
            TrimOptions        = TrimOptions.Trim,
            BadDataFound       = null,          // ignora líneas corruptas
            MissingFieldFound  = null           // ignora campos faltantes
        };

        using var reader = new StreamReader(ruta);
        using var csv    = new CsvReader(reader, cfg);

        /* Si hay cabecera declarada, intento leerla; si no, se seguirá por índice */
        if (s.header)
        {
            csv.Read();
            csv.ReadHeader();
        }

        while (csv.Read())
        {
            foreach (var sec in s.Sections)
            {
                var fila = dt.NewRow();

                for (int i = 0; i < sec.Fields.Count; i++)
                {
                    var campo = sec.Fields[i];
                    string valor;

                    if (s.header && csv.HeaderRecord?.Length > 0)
                    {
                        // 1º intento por nombre; si no existe, por índice
                        if (!csv.TryGetField(campo.fName, out valor))
                            valor = csv.GetField(i);
                    }
                    else
                    {
                        // No hay cabecera: siempre por índice
                        valor = csv.GetField(i);
                    }

                    fila[campo.fName] = valor ?? string.Empty;
                }

                dt.Rows.Add(fila);
            }
        }
        return dt;
    }

    /*───────────────────────────  Helpers  ─────────────────────────*/
    private static DataTable CrearDataTable(EsquemaArchivo s)
    {
        var dt = new DataTable();
        foreach (var campo in s.Sections.SelectMany(x => x.Fields))
            dt.Columns.Add(campo.fName, typeof(string));   // se tipificará más adelante
        return dt;
    }

    private static string UnescapeSeparator(string raw) =>
        raw switch
        {
            "\\t" => "\t",
            "\\|" => "|",
            "\\&" => "&",
            _     => raw
        };
}
