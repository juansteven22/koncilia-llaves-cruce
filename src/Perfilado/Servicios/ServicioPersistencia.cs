using Dapper;
using Microsoft.Data.SqlClient;
using Perfilado.Modelos;

namespace Perfilado.Servicios;

public class ServicioPersistencia
{
    private readonly SqlConnection _cnn;
    public ServicioPersistencia(SqlConnection cnn) => _cnn = cnn;

    public void Guardar(List<MetricaColumna> cols, List<MetricaCombinacion> combs)
    {
        const string insCol = """
          INSERT INTO ColumnProfile
            (Tabla, Columna, TipoDet, TotalFilas, Nulos, Cardinalidad, PorcUnico,
             LongMax, LongMin, PatronRegex, Media, P95)
          VALUES (@Tabla,@Columna,@TipoDet,@TotalFilas,@Nulos,@Cardinalidad,@PorcUnico,
                  @LongMax,@LongMin,@PatrónRegex,@Media,@P95);
        """;
        _cnn.Execute(insCol, cols);

        const string insComb = """
          INSERT INTO KeyCandidateFeatures
            (Tabla, Columnas, Cardinalidad, PorcUnico, HashSet)
          VALUES (@Tabla, @ColsCsv, @Cardinalidad, @PorcUnico, @HashSet);
        """;
        _cnn.Execute(insComb,
            combs.Select(c => new
            {
                c.Tabla,
                ColsCsv = string.Join(",", c.Columnas),
                c.Cardinalidad,
                c.PorcUnico,
                c.HashSet
            }));
    }
}
