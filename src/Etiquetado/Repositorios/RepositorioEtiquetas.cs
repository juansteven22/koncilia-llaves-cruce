// Repositorios/RepositorioEtiquetas.cs
// En primera persona: encapsulo todo el acceso a SQL con Dapper.
// Implemento Upsert por HashEtiqueta (evita duplicados de la misma llave).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Etiquetado.Modelos;
using Etiquetado.Modelos.Dtos;
using Etiquetado.Utils;

namespace Etiquetado.Repositorios;

public class RepositorioEtiquetas
{
    private readonly SqlConnection _cnn;
    public RepositorioEtiquetas(SqlConnection cnn) { _cnn = cnn; }

    public async Task<int> CrearOActualizarAsync(EtiquetaCreateDto dto)
    {
        // Normalizo entradas
        var ta = Normalizacion.Tabla(dto.TablaA);
        var tb = Normalizacion.Tabla(dto.TablaB);
        var ca = Normalizacion.ColumnasCsv(dto.ColumnasA);
        var cb = Normalizacion.ColumnasCsv(dto.ColumnasB);
        var hash = Normalizacion.HashEtiqueta(ta, ca, tb, cb);

        // Intento actualizar si existe; si no, inserto
        const string upsert = """
        MERGE dbo.KeyLabels AS T
        USING (SELECT @HashEtiqueta AS HashEtiqueta) AS S
        ON (T.HashEtiqueta = S.HashEtiqueta)
        WHEN MATCHED THEN
          UPDATE SET EsLlave = @EsLlave,
                     Justificacion = @Justificacion,
                     Usuario = @Usuario,
                     FechaActualizacion = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
          INSERT (HashEtiqueta, TablaA, ColumnasA, TablaB, ColumnasB, EsLlave, Justificacion, Usuario)
          VALUES (@HashEtiqueta, @TablaA, @ColumnasA, @TablaB, @ColumnasB, @EsLlave, @Justificacion, @Usuario)
        OUTPUT INSERTED.Id;
        """;

        var id = await _cnn.ExecuteScalarAsync<int>(upsert, new
        {
            HashEtiqueta = hash,
            TablaA = ta, ColumnasA = ca,
            TablaB = tb, ColumnasB = cb,
            dto.EsLlave, dto.Justificacion, dto.Usuario
        });

        return id;
    }

    public async Task<IEnumerable<EtiquetaDto>> BuscarAsync(string? tablaA, string? tablaB)
    {
        var sql = """
        SELECT Id, HashEtiqueta, TablaA, ColumnasA, TablaB, ColumnasB,
               EsLlave, Justificacion, Usuario,
               CONVERT(varchar(33), FechaCreacion, 126) AS FechaCreacion,
               CONVERT(varchar(33), FechaActualizacion, 126) AS FechaActualizacion
        FROM dbo.KeyLabels
        WHERE (@TablaA IS NULL OR TablaA = @TablaA)
          AND (@TablaB IS NULL OR TablaB = @TablaB)
        ORDER BY Id DESC;
        """;

        var datos = await _cnn.QueryAsync(sql, new
        {
            TablaA = string.IsNullOrWhiteSpace(tablaA) ? null : Normalizacion.Tabla(tablaA),
            TablaB = string.IsNullOrWhiteSpace(tablaB) ? null : Normalizacion.Tabla(tablaB)
        });

        // Mapeo a DTO de salida separando columnas CSV → string[]
        return datos.Select(r => new EtiquetaDto
        {
            Id = r.Id,
            TablaA = r.TablaA,
            ColumnasA = ((string)r.ColumnasA).Split(',', StringSplitOptions.RemoveEmptyEntries),
            TablaB = r.TablaB,
            ColumnasB = ((string)r.ColumnasB).Split(',', StringSplitOptions.RemoveEmptyEntries),
            EsLlave = r.EsLlave,
            Justificacion = r.Justificacion,
            Usuario = r.Usuario,
            HashEtiqueta = r.HashEtiqueta,
            FechaCreacion = r.FechaCreacion,
            FechaActualizacion = r.FechaActualizacion
        });
    }

    public async Task<EtiquetaDto?> ObtenerPorIdAsync(int id)
    {
        var sql = """
        SELECT Id, HashEtiqueta, TablaA, ColumnasA, TablaB, ColumnasB,
               EsLlave, Justificacion, Usuario,
               CONVERT(varchar(33), FechaCreacion, 126) AS FechaCreacion,
               CONVERT(varchar(33), FechaActualizacion, 126) AS FechaActualizacion
        FROM dbo.KeyLabels
        WHERE Id = @Id;
        """;

        var r = await _cnn.QuerySingleOrDefaultAsync(sql, new { Id = id });
        if (r is null) return null;

        return new EtiquetaDto
        {
            Id = r.Id,
            TablaA = r.TablaA,
            ColumnasA = ((string)r.ColumnasA).Split(',', StringSplitOptions.RemoveEmptyEntries),
            TablaB = r.TablaB,
            ColumnasB = ((string)r.ColumnasB).Split(',', StringSplitOptions.RemoveEmptyEntries),
            EsLlave = r.EsLlave,
            Justificacion = r.Justificacion,
            Usuario = r.Usuario,
            HashEtiqueta = r.HashEtiqueta,
            FechaCreacion = r.FechaCreacion,
            FechaActualizacion = r.FechaActualizacion
        };
    }

    public async Task<bool> ActualizarAsync(int id, EtiquetaUpdateDto dto)
    {
        var sql = """
        UPDATE dbo.KeyLabels
           SET EsLlave = @EsLlave,
               Justificacion = @Justificacion,
               Usuario = @Usuario,
               FechaActualizacion = SYSUTCDATETIME()
         WHERE Id = @Id;
        """;
        int rows = await _cnn.ExecuteAsync(sql, new
        {
            Id = id,
            dto.EsLlave,
            dto.Justificacion,
            dto.Usuario
        });
        return rows > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var sql = "DELETE FROM dbo.KeyLabels WHERE Id = @Id;";
        int rows = await _cnn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
