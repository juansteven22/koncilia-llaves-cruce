// ----------------------------------------------------------------------------
// ServicioPerfilador.cs
// ----------------------------------------------------------------------------
// Este servicio recibe el nombre de una tabla cruda (raw_*), la lee a memoria
// como DataTable y calcula:
//
//   1.  Métricas básicas *por columna*  (ColumnProfile)
//   2.  Lista filtrada de *columnas candidatas*  (alto % de unicidad, pocos nulos)
//   3.  Métricas de *combinaciones de columnas*  (1-3 columnas) usando
//       ServicioCombinaciones.
//
// El resultado se devuelve como dos listas en memoria; otro servicio (Persistencia)
// es el responsable de guardarlas en SQL.
//

// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;                    // ▸ LINQ: operaciones tipo SQL sobre colecciones
using Dapper;                         // ▸ para rellenar DataTable vía SqlDataAdapter
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Perfilado.Modelos;
using Perfilado.Utils;

namespace Perfilado.Servicios;

public class ServicioPerfilador
{
    private readonly SqlConnection   _cnn; // conexión viva a SQL Server
    private readonly IConfiguration  _cfg; // para leer parámetros de appsettings

    public ServicioPerfilador(SqlConnection cnn, IConfiguration cfg)
    {
        _cnn = cnn;
        _cfg = cfg;
    }

    /// <summary>
    /// Punto de entrada: calcula métricas de tablaRaw y devuelve dos listas.
    /// </summary>
    public (List<MetricaColumna> cols, List<MetricaCombinacion> comb)
        Perfilar(string tablaRaw)
    {
        // Lee toda la tabla (o una muestra) a un DataTable en RAM
        DataTable dt = CargarDataTable(tablaRaw);

        long total = dt.Rows.Count;                 // nº de filas totales
        var lstCol = new List<MetricaColumna>();    // lista de métricas por columna

        // ──────────────── 1. Métricas de cada columna ────────────────
        foreach (DataColumn dc in dt.Columns)       // bucle sobre columnas
        {
            // ► Extraemos la columna como IEnumerable<object>
            var valores = dt.AsEnumerable()         // DataTable → IEnumerable<DataRow>
                            .Select(r => r[dc])     // cada fila devuelve el valor de esta columna
                            .ToList();              // materializamos en memoria

            // --- Métricas simples -----------------------------------
            long nulos = valores.Count(v => v == DBNull.Value);

            // Cardinalidad: nº de valores distintos
            long card = Cardinalidad(valores);

            // % unicidad redondeado a 2 decimales
            double porcUnico = Math.Round(card * 100.0 / total, 2);

            // --- Métricas especiales para texto ---------------------
            int maxLen = 0, minLen = int.MaxValue;
            string regexPred = "VARIOS";            // valor por defecto

            if (dc.DataType == typeof(string))
            {
                var textos = valores          // convertimos solo valores NO nulos a string
                            .Where(v => v != DBNull.Value)
                            .Select(v => v.ToString()!)
                            .ToList();

                if (textos.Any())
                {
                    maxLen = textos.Max(t => t.Length);
                    minLen = textos.Min(t => t.Length);

                    // Heurística de patrón predominante
                    if (textos.All(t => t.All(char.IsDigit)))        regexPred = @"^\d+$";
                    else if (textos.All(t => t.All(char.IsLetter)))  regexPred = @"^[A-Za-z]+$";
                }
                else minLen = 0;
            }

            // --- Métricas numéricas (media, p95) --------------------
            double? media = null, p95 = null;

            if (EsNumerico(dc.DataType))
            {
                var nums = valores
                           .Where(v => v != DBNull.Value)
                           .Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture))
                           .ToList();

                if (nums.Any())
                {
                    media = nums.Average();
                    p95   = Percentil(nums, 0.95);
                }
            }

            // ► Creamos el record con TODAS las métricas calculadas
            lstCol.Add(new MetricaColumna(
                tablaRaw,                    // tabla de origen
                dc.ColumnName,               // nombre de la columna
                dc.DataType.Name,            // tipo detectado
                total, nulos, card, porcUnico,
                maxLen, minLen == int.MaxValue ? 0 : minLen,
                regexPred, media, p95));
        }

        // ──────────────── 2. Selección de columnas candidatas ────────
        // Criterios de selección vienen de appsettings.json
        double umbralUni  = _cfg.GetSection("Perfilado").GetValue<double>("UmbralUnicidadCol"); // p.e. 0.2 (20 %)
        double umbralNull = _cfg.GetSection("Perfilado").GetValue<double>("UmbralNulos");       // p.e. 0.5 (50 %)

        // LINQ: filtramos la lista de métricas para obtener solo los nombres
        var candidatas = lstCol
            .Where(c =>
                    c.PorcUnico >= umbralUni * 100 &&      // unicidad suficiente
                    c.Nulos     <= umbralNull * c.TotalFilas &&
                    c.Cardinalidad > 1)                    // descarta columnas constantes
            .Select(c => c.Columna)
            .ToArray();                                   // convertimos a array string[]

        // ──────────────── 3. Cálculo de combinaciones ────────────────
        int maxK   = _cfg.GetSection("Perfilado").GetValue<int>("MaxColsCombinacion"); // 3
        int maxAll = _cfg.GetSection("Perfilado").GetValue<int>("MaxCombPerTabla");    // 5 000

        // Instanciamos ServicioCombinaciones y le delegamos el cálculo pesado
        var combSvc = new ServicioCombinaciones();
        var lstComb = combSvc.Generar(dt, tablaRaw, maxK, candidatas, maxAll);

        // Devolvemos ambas listas a quien lo llame (Program -> Persistencia)
        return (lstCol, lstComb);
    }

    //──────────────────────── helpers privados ──────────────────────────────

    /// <summary>Lee la tabla (o muestra) a DataTable usando SqlDataAdapter.</summary>
    private DataTable CargarDataTable(string tabla)
    {
        long limite = _cfg.GetSection("Perfilado").GetValue<long>("MuestraMaxFilas"); // e.g. 500000
        // TABLESAMPLE mejora tiempos en tablas enormes
        string sql = $"""
          SELECT *
          FROM {tabla}
          {(limite > 0 ? $"TABLESAMPLE ({limite} ROWS)" : "")}
        """;

        using var da = new SqlDataAdapter(sql, _cnn);
        var dt = new DataTable();
        da.Fill(dt);   // llena el DataTable con el resultado del query
        return dt;
    }

    /// <summary>Cuenta valores distintos convirtiéndolos a string.</summary>
    private static long Cardinalidad(IEnumerable<object> valores) =>
        valores.Select(v => v?.ToString() ?? "").Distinct().LongCount();

    /// <summary>Chequea si un System.Type es de tipo numérico “simple”.</summary>
    private static bool EsNumerico(Type t) =>
        t == typeof(int)    || t == typeof(long)  ||
        t == typeof(float)  || t == typeof(double)||
        t == typeof(decimal);

    /// <summary>Percentil simple usando interpolación lineal.</summary>
    private static double Percentil(List<double> nums, double p)
    {
        nums.Sort();                                 // orden asc
        double pos  = (nums.Count - 1) * p;         // posición real
        int idx     = (int)pos;                     // parte entera
        double frac = pos - idx;                    // parte decimal

        return idx + 1 < nums.Count
            ? nums[idx] + frac * (nums[idx + 1] - nums[idx])   // interpol.
            : nums[idx];                                       // último valor
    }
}
