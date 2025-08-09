// Program.cs — Servicio Etiquetado (Web API mínima)
// En primera persona explico qué hago en cada paso:
// - Configuro DI, logging, Swagger.
// - Registro la conexión SQL.
// - Aseguro DDL (tabla KeyLabels) si está habilitado.
// - Expongo endpoints REST para crear/listar/actualizar/borrar etiquetas.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using Etiquetado.Endpoints;
using Etiquetado.Repositorios;
using Etiquetado.Servicios;

var builder = WebApplication.CreateBuilder(args);

// 1) Configuración: leo appsettings y preparo Swagger para probar en local
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2) Registro la conexión SQL inyectable
builder.Services.AddTransient<SqlConnection>(_ =>
{
    // En primera persona: aquí obtengo la cadena de conexión "SqlServer"
    var cs = builder.Configuration.GetConnectionString("SqlServer");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Cadena 'SqlServer' no configurada.");
    return new SqlConnection(cs);
});

// 3) Registro mis servicios propios (repositorio + DDL)
builder.Services.AddScoped<RepositorioEtiquetas>();
builder.Services.AddScoped<ServicioTablaSqlMeta>();

var app = builder.Build();

// 4) Swagger solo en desarrollo (también puedes dejarlo siempre en local)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5) Si la bandera lo indica, creo el DDL de KeyLabels al arrancar
if (app.Configuration.GetSection("Etiquetado").GetValue<bool>("CrearDDLAlArrancar"))
{
    using var scope = app.Services.CreateScope();
    var meta = scope.ServiceProvider.GetRequiredService<ServicioTablaSqlMeta>();
    meta.AsegurarDDL();
}

// 6) Mapear endpoints REST
app.MapEtiquetasEndpoints(); // extensión que define /api/etiquetas

app.Run();
