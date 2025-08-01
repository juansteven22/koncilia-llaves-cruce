// Program.cs — Servicio Ingesta
// 01-ago-2025

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ingesta.Modelos;
using Ingesta.Utils;   // nuevo using



var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        c.AddEnvironmentVariables();                 // Permite sobreescribir en prod
    })
    .ConfigureLogging(l => l.AddConsole())
    .ConfigureServices((ctx, services) =>
    {
        // ► Registro de servicios propios
        services.AddSingleton<ServicioEsquema>();
        services.AddSingleton<ServicioParser>();
        services.AddSingleton<ServicioCargaSql>();

        // ► Factory de SqlConnection (obliga a tener cadena en appsettings.json)
        services.AddTransient<SqlConnection>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var cadena = cfg.GetConnectionString("SqlServer")
                         ?? throw new InvalidOperationException("Cadena SQL ausente");
            return new SqlConnection(cadena);
        });

        // ► ServicioTablaSql necesita ILogger y llega el SqlConnection vía Activator
        services.AddTransient<ServicioTablaSql>();
    })
    .Build();

/*---------- CLI mínimo: comprobamos argumentos ----------*/
if (args.Length < 2)
{
    Console.WriteLine("""
        Uso:
          dotnet run -- <rutaArchivoDatos> <rutaEsquemaJson>

        Ejemplo:
          dotnet run -- "C:\Data\archivos\TIN000000000001032.20230725.20230725.txt" `
                      "C:\Data\archivos\TIN.json"
        """);
    return;
}

string rutaArchivo = Path.GetFullPath(args[0]);
string rutaJson    = Path.GetFullPath(args[1]);
//string nombreTabla = $"raw_{Path.GetFileNameWithoutExtension(rutaArchivo)}";


// ...
string archivoBase = Path.GetFileNameWithoutExtension(rutaArchivo);
string nombreTabla  = $"raw_{NombreSeguro.Limpiar(archivoBase)}";   // ← limpio

var log = host.Services.GetRequiredService<ILogger<Program>>();
log.LogInformation("Archivo   : {file}", rutaArchivo);
log.LogInformation("Esquema   : {json}", rutaJson);
log.LogInformation("Tabla destino: {tabla}", nombreTabla);

/*---------- Pipeline de ingesta ----------*/
var esquemaSvc = host.Services.GetRequiredService<ServicioEsquema>();
var parserSvc  = host.Services.GetRequiredService<ServicioParser>();

EsquemaArchivo esquema;
try
{
    esquema = esquemaSvc.Cargar(rutaJson);
}
catch (Exception ex)
{
    log.LogError(ex, "No pude leer el JSON de esquema.");
    return;
}

DataTable datos;
try
{
    datos = parserSvc.Parsear(rutaArchivo, esquema);
}
catch (Exception ex)
{
    log.LogError(ex, "Error al parsear el archivo de datos.");
    return;
}

/*---------- Alcance DI para usar SqlConnection ----------*/
await using var scope = host.Services.CreateAsyncScope();
var cnn = scope.ServiceProvider.GetRequiredService<SqlConnection>();
await cnn.OpenAsync();

var tblSvc   = scope.ServiceProvider.GetRequiredService<ServicioTablaSql>();
var tablaDestino = tblSvc.AsegurarTabla(nombreTabla, 
                    datos.Columns.Cast<DataColumn>()
                         .Select(c => c.ColumnName));

var cargaSvc = scope.ServiceProvider.GetRequiredService<ServicioCargaSql>();
cargaSvc.BulkInsert(cnn, tablaDestino, datos);

log.LogInformation("✓ Proceso de ingesta finalizado correctamente en {tabla}", tablaDestino);
