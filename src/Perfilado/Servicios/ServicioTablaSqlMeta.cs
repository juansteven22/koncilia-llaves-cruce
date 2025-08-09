using Dapper;
using Microsoft.Data.SqlClient;

namespace Perfilado.Servicios;

public class ServicioTablaSqlMeta
{
    private readonly SqlConnection _cnn;
    public ServicioTablaSqlMeta(SqlConnection cnn) => _cnn = cnn;

    public void AsegurarDDL()
    {
        _cnn.Execute("""
        IF OBJECT_ID('ColumnProfile','U') IS NULL
          CREATE TABLE ColumnProfile(
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Tabla NVARCHAR(256),
            Columna NVARCHAR(128),
            TipoDet NVARCHAR(50),
            TotalFilas BIGINT,
            Nulos BIGINT,
            Cardinalidad BIGINT,
            PorcUnico DECIMAL(5,2),
            LongMax INT,
            LongMin INT,
            PatronRegex NVARCHAR(200),
            Media FLOAT NULL,
            P95 FLOAT NULL
          );
        IF OBJECT_ID('KeyCandidateFeatures','U') IS NULL
          CREATE TABLE KeyCandidateFeatures(
            Id INT IDENTITY(1,1) PRIMARY KEY,
            Tabla NVARCHAR(256),
            Columnas NVARCHAR(400),
            Cardinalidad BIGINT,
            PorcUnico DECIMAL(5,2),
            HashSet CHAR(64)
          );
        """);
    }
}
