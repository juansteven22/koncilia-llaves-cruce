// -----------------------------------------------------------------------------
// Modelos/MetricaCombinacion.cs
// -----------------------------------------------------------------------------
// Este record describe las MÉTRICAS de una **combinación de columnas** dentro
// de una misma tabla cruda.  Cada fila generada por el servicio de Perfilado
// termina en la tabla SQL   dbo.KeyCandidateFeatures
//
// Ejemplo de uso:
//
//   var comb = new MetricaCombinacion(
//       "dbo.raw_TIN000000000001032_20230725_20230725",   // Tabla
//       new[] { "CUS", "Valor" },                        // Pareja de columnas
//       98_700,                                          // Cardinalidad
//       99.9,                                            // % unicidad
//       "7D5F3BE3…"                                      // Hash fingerprint
//   );
//
// ▸ Posteriormente el módulo Predicción cruzará esta fila con otra combinación
//   de la tabla B para estimar si ambas forman una llave válida.
// -----------------------------------------------------------------------------
// Nota sobre HashSet:  Aquí NO se guarda el conjunto; se guarda un fingerprint
//   (SHA-256, MinHash, etc.) que sirve como “firma” para calcular similitud
//   entre tablas sin mover todos los valores crudos.
// -----------------------------------------------------------------------------

namespace Perfilado.Modelos;

/// <summary>
/// Métricas de perfilado de una **combinación** de columnas (k=1..3).
/// </summary>
/// <param name="Tabla">
///   Nombre completo de la tabla origen (ej.: dbo.raw_BCSDEALME0725).
/// </param>
/// <param name="Columnas">
///   Array con los nombres de las columnas que integran la combinación.
///   El orden se conserva, p. ej. ["Id","Valor"].
/// </param>
/// <param name="Cardinalidad">
///   Número de claves distintas que resultan al concatenar las columnas.
/// </param>
/// <param name="PorcUnico">
///   Cardinalidad expresada como porcentaje del total de filas
///   (100 % = la combinación es única, buen candidato a llave primaria).
/// </param>
/// <param name="HashSet">
///   Fingerprint del conjunto (SHA-256 del listado de columnas o MinHash
///   de los valores), usado para comparar combinaciones entre tablas sin
///   transferir datos completos.
/// </param>
public record MetricaCombinacion(
    string   Tabla,
    string[] Columnas,
    long     Cardinalidad,
    double   PorcUnico,
    string   HashSet
);
