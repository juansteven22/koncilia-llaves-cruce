// Servicios/ServicioTablaSqlMeta.cs
using Dapper;
using Microsoft.Data.SqlClient;

namespace Etiquetado.Servicios;

public class ServicioTablaSqlMeta
{
    private readonly SqlConnection _cnn;
    public ServicioTablaSqlMeta(SqlConnection cnn) { _cnn = cnn; }

    public void AsegurarDDL()
    {
        _cnn.Open();
        _cnn.Execute("""
        IF OBJECT_ID('dbo.KeyLabels','U') IS NULL
        BEGIN
          CREATE TABLE dbo.KeyLabels(
            Id                 INT IDENTITY(1,1) PRIMARY KEY,
            HashEtiqueta       CHAR(64) NOT NULL,
            TablaA             NVARCHAR(256) NOT NULL,
            ColumnasA          NVARCHAR(400) NOT NULL,
            TablaB             NVARCHAR(256) NOT NULL,
            ColumnasB          NVARCHAR(400) NOT NULL,
            EsLlave            BIT NOT NULL,
            Justificacion      NVARCHAR(1000) NULL,
            Usuario            NVARCHAR(100) NULL,
            FechaCreacion      DATETIME2 NOT NULL CONSTRAINT DF_KeyLabels_FC DEFAULT SYSUTCDATETIME(),
            FechaActualizacion DATETIME2 NULL
          );
          CREATE UNIQUE INDEX UX_KeyLabels_Hash ON dbo.KeyLabels(HashEtiqueta);
        END
        """);
    }
}
