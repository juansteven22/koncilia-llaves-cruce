// Program.cs — Servicio Ingesta
// Versión “robusta” 02-ago-2025
// Autor: Juan Steven Montenegro Penagos
//
// Resumen de pipeline:
//
//  1. Leer archivo de datos + JSON-esquema.
//  2. Parsear (CSV, FIXED) → DataTable.
//  3. Deducir tipos SQL con muestra (≤100 filas).
//  4. Ajustar NVARCHAR usando longitud total real y fLength del JSON.
//  5. Ajustar numéricos: INT→BIGINT→DECIMAL si los dígitos lo exigen.
//  6. DROP TABLE IF EXISTS + CREATE TABLE.
//  7. Convertir datos (""/NA → NULL, string→long/decimal/DateTime).
//  8. SqlBulkCopy de todas las filas.

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ingesta.Modelos;
using Ingesta.Servicios;
using Ingesta.Utils;              // InferidorTipos, Coercion, PostCheckNumericos

namespace Ingesta;

public class Program
{
    public static async Task Main(string[] args)
    {
        /*─────────────────────── BOOTSTRAP HOST ───────────────────────*/
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(cfg =>
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables())
            .ConfigureLogging(l => l.AddConsole())
            .ConfigureServices((ctx, s) =>
            {
                s.AddSingleton<ServicioEsquema>();
                s.AddSingleton<ServicioParser>();
                s.AddSingleton<ServicioCargaSql>();
                s.AddTransient<SqlConnection>(_ =>
                    new SqlConnection(ctx.Configuration.GetConnectionString("SqlServer") ??
                        throw new InvalidOperationException("Falta cadena 'SqlServer'")));
                s.AddTransient<ServicioTablaSql>();
            })
            .Build();

        /*──────────────────────── CLI INPUT CHECK ─────────────────────*/
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: dotnet run -- <archivoDatos> <esquemaJson>");
            return;
        }

        string rutaDatos = Path.GetFullPath(args[0]);
        string rutaJson  = Path.GetFullPath(args[1]);
        string tablaBase = $"raw_{Path.GetFileNameWithoutExtension(rutaDatos)}";

        var log = host.Services.GetRequiredService<ILogger<Program>>();
        log.LogInformation("Archivo   : {file}", rutaDatos);
        log.LogInformation("Esquema   : {json}", rutaJson);

        /*──────────────────────── PARSEO ARCHIVO ──────────────────────*/
        var esquemaSvc = host.Services.GetRequiredService<ServicioEsquema>();
        var parserSvc  = host.Services.GetRequiredService<ServicioParser>();

        EsquemaArchivo esquema = esquemaSvc.Cargar(rutaJson);
        DataTable datos        = parserSvc.Parsear(rutaDatos, esquema);

        /*───────────────── DEDUCCIÓN + AJUSTES DE TIPOS ───────────────*/
        var metaCampos = esquema.Sections
                                .SelectMany(s => s.Fields)
                                .ToDictionary(f => f.fName);

        // 3) muestra de 100 filas
        var tipos = InferidorTipos.Inferir(datos, 100);

        // 4) NVARCHAR: longitud real total y fLength JSON
        tipos = tipos.Select(t =>
        {
            if (!t.SqlType.StartsWith("NVARCHAR(")) return t;

            int realLen = datos.AsEnumerable()
                               .Select(r => r[t.Col]?.ToString()?.Length ?? 0)
                               .Max();

            int jsonLen = metaCampos.TryGetValue(t.Col, out var m) ? m.fLength : 0;
            int tamaño  = Math.Max(realLen, jsonLen);

            int ancho   = tamaño switch
            {
                <= 0   => 1,          // nunca 0
                > 4000 => 0,          // 0 marca NVARCHAR(MAX)
                _      => tamaño
            };

            string sql = ancho == 0 ? "NVARCHAR(MAX)" : $"NVARCHAR({ancho})";
            return (t.Col, sql);
        }).ToArray();

        // 5) INT→BIGINT→DECIMAL según dígitos reales
        tipos = PostCheckNumericos.Ajustar(datos, tipos);

        /*───────────────── CREAR TABLA + CARGA MASIVA ─────────────────*/
        await using var scope = host.Services.CreateAsyncScope();
        var cnn = scope.ServiceProvider.GetRequiredService<SqlConnection>();
        await cnn.OpenAsync();

        var tablaSvc = scope.ServiceProvider.GetRequiredService<ServicioTablaSql>();
        string tablaDestino = tablaSvc.AsegurarTabla(tablaBase, tipos);

        // 6) Conversión de datos
        Coercion.Convertir(datos, tipos);

        var cargaSvc = scope.ServiceProvider.GetRequiredService<ServicioCargaSql>();
        cargaSvc.BulkInsert(cnn, tablaDestino, datos);

        log.LogInformation("✓ {rows} filas insertadas en {tabla}",
                           datos.Rows.Count, tablaDestino);
    }
}
