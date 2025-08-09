// ServicioTablaSql.cs
// Crea SIEMPRE la tabla (la borra primero si existía)

using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Ingesta.Utils;

namespace Ingesta.Servicios;

public class ServicioTablaSql
{
    private readonly SqlConnection _cnn;
    private readonly ILogger<ServicioTablaSql> _log;

    public ServicioTablaSql(SqlConnection cnn, ILogger<ServicioTablaSql> log)
    {
        _cnn = cnn;
        _log = log;
    }

    /// <summary>
    /// Elimina la tabla si ya existe y la crea de nuevo
    /// </summary>
    public string AsegurarTabla(string tablaBase, IEnumerable<(string Col, string SqlType)> columnas)
    {
        string tablaSegura  = NombreSeguro.Limpiar(tablaBase);
        string tablaDestino = $"[dbo].[{tablaSegura}]";

        /* 1️⃣  DROP TABLE IF EXISTS */
        string drop = $"IF OBJECT_ID('{tablaDestino}', 'U') IS NOT NULL DROP TABLE {tablaDestino}";
        _cnn.Execute(drop);
        _log.LogInformation("Tabla {tabla} eliminada (si existía).", tablaDestino);

        /* 2️⃣  CREATE TABLE nueva */
        string columnasSql = string.Join(", ",
            columnas.Select(c => $"[{c.Col}] {c.SqlType} NULL"));

        string ddl = $"""
            CREATE TABLE {tablaDestino} (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                {columnasSql}
            );
        """;

        _log.LogInformation("Creo tabla {tabla}…", tablaDestino);
        _cnn.Execute(ddl);

        return tablaDestino;
    }
}
