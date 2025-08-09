// Program.cs — Servicio Perfilado
// Carga una tabla cruda, calcula métricas de perfilado
// y las persiste en ColumnProfile / KeyCandidateFeatures.
//
// Uso CLI:
//   dotnet run -- <nombreTablaRaw>
//
// Ejemplo:
//   dotnet run -- "dbo.raw_TIN000000000001032_20230725_20230725"
using System.Diagnostics;   // ← para Stopwatch

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Perfilado.Servicios;

namespace Perfilado;

public class Program
{
    public static async Task Main(string[] args)
    {
        /*──────────────────────  Construyo Host  ──────────────────────*/
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(cfg =>
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables())
            .ConfigureLogging(l => l.AddConsole())
            .ConfigureServices((ctx, s) =>
            {
                // Servicios del módulo
                s.AddSingleton<ServicioPerfilador>();
                s.AddSingleton<ServicioPersistencia>();
                s.AddSingleton<ServicioTablaSqlMeta>();

                // SqlConnection inyectable
                s.AddTransient<SqlConnection>(_ =>
                    new SqlConnection(ctx.Configuration.GetConnectionString("SqlServer") ??
                        throw new InvalidOperationException("Cadena 'SqlServer' no configurada")));
            })
            .Build();

        /*──────────────────────  Validación CLI  ──────────────────────*/
        if (args.Length < 1)
        {
            Console.WriteLine("Uso: dotnet run -- <tabla_raw>");
            return;
        }
        string tablaRaw = args[0];
        var log = host.Services.GetRequiredService<ILogger<Program>>();
        log.LogInformation("Iniciando perfilado de tabla {tabla}", tablaRaw);

        var cronometro = Stopwatch.StartNew();   // ⏱️ inicio


        /*──────────────────────  Alcance DI  ─────────────────────────*/
        await using var scope = host.Services.CreateAsyncScope();
        var cnn = scope.ServiceProvider.GetRequiredService<SqlConnection>();
        await cnn.OpenAsync();

        /* 1️⃣  Asegurar tablas de metadatos */
        var metaSvc = scope.ServiceProvider.GetRequiredService<ServicioTablaSqlMeta>();
        metaSvc.AsegurarDDL();

        /* 2️⃣  Calcular métricas */
        var perfSvc = scope.ServiceProvider.GetRequiredService<ServicioPerfilador>();
        var (cols, combs) = perfSvc.Perfilar(tablaRaw);

        /* 3️⃣  Persistir resultados */
        var persSvc = scope.ServiceProvider.GetRequiredService<ServicioPersistencia>();
        persSvc.Guardar(cols, combs);

        log.LogInformation("✓ Perfilado completado: {cols} columnas, {combs} combinaciones",
                           cols.Count, combs.Count);


        persSvc.Guardar(cols, combs);


//persSvc.Guardar(cols, combs);

cronometro.Stop();                                  // ⏱️ fin
var t = cronometro.Elapsed;
log.LogInformation(
    "Tiempo empleado: {Horas:D2}:{Minutos:D2}:{Segundos:D2}",
    t.Hours, t.Minutes, t.Seconds);

log.LogInformation("✓ Perfilado completado: {cols} columnas, {combs} combinaciones",
                   cols.Count, combs.Count);



    }
}
