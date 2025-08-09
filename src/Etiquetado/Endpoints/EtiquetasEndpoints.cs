// Endpoints/EtiquetasEndpoints.cs
// Aquí defino las rutas REST con Minimal API. En primera persona comento
// el porqué de cada endpoint y cómo valido parámetros.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Etiquetado.Modelos.Dtos;
using Etiquetado.Repositorios;

namespace Etiquetado.Endpoints;

public static class EtiquetasEndpoints
{
    public static void MapEtiquetasEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/etiquetas")
                          .WithTags("Etiquetas"); // visible en Swagger

        // POST /api/etiquetas → crear o "upsert" de una etiqueta
        group.MapPost("/", async (EtiquetaCreateDto dto, RepositorioEtiquetas repo) =>
        {
            // Valido entrada mínima
            if (string.IsNullOrWhiteSpace(dto.TablaA) ||
                string.IsNullOrWhiteSpace(dto.TablaB) ||
                dto.ColumnasA is null || dto.ColumnasA.Length == 0 ||
                dto.ColumnasB is null || dto.ColumnasB.Length == 0)
            {
                return Results.BadRequest("Faltan campos obligatorios (TablaA/TablaB/ColumnasA/ColumnasB).");
            }

            var id = await repo.CrearOActualizarAsync(dto);
            return Results.Ok(new { Id = id });
        })
        .WithSummary("Crea o actualiza (upsert) una etiqueta de llave entre dos tablas");

        // GET /api/etiquetas → listar (con filtros opcionales)
        group.MapGet("/", async (string? tablaA, string? tablaB, RepositorioEtiquetas repo) =>
        {
            var lista = await repo.BuscarAsync(tablaA, tablaB);
            return Results.Ok(lista);
        })
        .WithSummary("Lista etiquetas; se puede filtrar por tablaA/tablaB");

        // GET /api/etiquetas/{id}
        group.MapGet("/{id:int}", async (int id, RepositorioEtiquetas repo) =>
        {
            var e = await repo.ObtenerPorIdAsync(id);
            return e is null ? Results.NotFound() : Results.Ok(e);
        })
        .WithSummary("Obtiene una etiqueta por Id");

        // PUT /api/etiquetas/{id} → actualizar campos editables
        group.MapPut("/{id:int}", async (int id, EtiquetaUpdateDto dto, RepositorioEtiquetas repo) =>
        {
            var ok = await repo.ActualizarAsync(id, dto);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Actualiza una etiqueta existente");

        // DELETE /api/etiquetas/{id}
        group.MapDelete("/{id:int}", async (int id, RepositorioEtiquetas repo) =>
        {
            var ok = await repo.EliminarAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Elimina una etiqueta por Id");
    }
}
