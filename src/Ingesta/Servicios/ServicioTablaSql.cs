using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

public class ServicioTablaSql
{
    private readonly SqlConnection _cnn;
    private readonly ILogger<ServicioTablaSql> _log;

    public ServicioTablaSql(SqlConnection cnn, ILogger<ServicioTablaSql> log)
    {
        _cnn = cnn; _log = log;
    }

// …using…
// Cuando devuelvo el nombre, incluyo el esquema dbo
public string AsegurarTabla(string tablaSinEsquema, IEnumerable<string> columnas)
{
    string tablaDestino = $"[dbo].[{tablaSinEsquema}]";

    // resto del código igual, pero uso tablaDestino en la consulta Dapper
    var existe = _cnn.ExecuteScalar<int>(
        "SELECT COUNT(*) FROM sys.tables WHERE name = @n", new { n = tablaSinEsquema });

    if (existe == 0)
    {
        var colsSql = string.Join(", ", columnas.Select(c => $"[{c}] NVARCHAR(MAX)"));
        var ddl = $"""
            CREATE TABLE {tablaDestino} (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                {colsSql}
            );
        """;
        _log.LogInformation("Creo tabla {tabla}…", tablaDestino);
        _cnn.Execute(ddl);
    }
    return tablaDestino;      // devuelvo [dbo].[tabla_limpia]
}

}
