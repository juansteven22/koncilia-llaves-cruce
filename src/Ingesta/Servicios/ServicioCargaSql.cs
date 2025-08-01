using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

public class ServicioCargaSql
{
    private readonly ILogger<ServicioCargaSql> _log;

    public ServicioCargaSql(ILogger<ServicioCargaSql> log) => _log = log;

    public void BulkInsert(SqlConnection cnn, string tabla, DataTable data)
    {
        using var bulk = new SqlBulkCopy(cnn)
        {
            DestinationTableName = tabla,
            BatchSize = 10_000
        };
        // mapeo ya viene con esquema incluido
bulk.DestinationTableName = tabla;   // tabla == [dbo].[raw_TIN000000000001032_20230725_20230725]

        foreach (DataColumn col in data.Columns)
            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        _log.LogInformation("Insertando {rows} filas en {tabla}…", data.Rows.Count, tabla);
        bulk.WriteToServer(data);
    }
}
