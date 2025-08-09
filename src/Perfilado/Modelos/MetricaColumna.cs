// -----------------------------------------------------------------------------
// Modelos/MetricaColumna.cs
// -----------------------------------------------------------------------------
// Este record representa el “carné de identidad” de **una columna** dentro
// de una tabla cruda.  Cada propiedad guarda una métrica calculada por el
// servicio de Perfilado; todas las filas resultantes van a la tabla SQL
//   dbo.ColumnProfile
// -----------------------------------------------------------------------------
// ▸ Uso típico:
//
//   var metrica = new MetricaColumna(
//        "dbo.raw_BCSDEALME0725",    // Tabla origen
//        "IdUnico",                  // Nombre de la columna
//        "String",                   // Tipo detectado
//        300_000,                    // TotalFilas
//        0,                          // Nulos
//        300_000,                    // Cardinalidad (todos únicos)
//        100.0,                      // Porcentaje de unicidad
//        20,                         // Longitud máxima
//        20,                         // Longitud mínima
//        @"^\d+$",                   // Patrón regex predominante
//        null,                       // Media (no aplica a texto)
//        null                        // P95  (no aplica a texto)
//   );
//
// -----------------------------------------------------------------------------
// Nota:  Se usa **record** para aprovechar inmutabilidad y comparaciones
//        estructurales en C# 9+.
// -----------------------------------------------------------------------------

namespace Perfilado.Modelos;

/// <summary>
/// Métricas de perfilado de UNA columna.
/// </summary>
/// <param name="Tabla">Nombre completo de la tabla cruda (ej. dbo.raw_X).</param>
/// <param name="Columna">Nombre de la columna analizada.</param>
/// <param name="TipoDet">Tipo detectado (String, Int32, Double, DateTime…).</param>
/// <param name="TotalFilas">Número total de registros evaluados.</param>
/// <param name="Nulos">Cuántos valores nulos o vacíos existen.</param>
/// <param name="Cardinalidad">
///   Número de valores distintos encontrados (sin contar nulos).
/// </param>
/// <param name="PorcUnico">
///   Cardinalidad expresada como porcentaje del total
///   (100 % = columna única).
/// </param>
/// <param name="LongMax">
///   Longitud máxima de las cadenas (solo para columnas texto).
/// </param>
/// <param name="LongMin">
///   Longitud mínima de las cadenas (0 si todas nulas o si no es texto).
/// </param>
/// <param name="PatrónRegex">
///   Expresión regular simplificada que describe el patrón dominante
///   (ej.: todo dígito, todo letra, «VARIOS» si no se pudo inferir).
/// </param>
/// <param name="Media">
///   Media aritmética (solo para columnas numéricas; null en texto/fecha).
/// </param>
/// <param name="P95">
///   Percentil 95 % (solo numérico; null en texto/fecha).
/// </param>
public record MetricaColumna(
    string  Tabla,
    string  Columna,
    string  TipoDet,
    long    TotalFilas,
    long    Nulos,
    long    Cardinalidad,
    double  PorcUnico,
    int     LongMax,
    int     LongMin,
    string  PatrónRegex,
    double? Media,
    double? P95
);
