// ServicioCombinaciones.cs ----------------------------------------------------
// Genera combinaciones de columnas (k = 1..maxK) sobre una tabla DataTable
// y calcula, para cada combinación, su cardinalidad y % de unicidad.
// Solo conserva las combinaciones “fuertes” según umbrales configurados
// (≥ 1 columna altamente única y al menos 30 % de unicidad total).
// ---------------------------------------------------------------------------

using System;                         // Tipos básicos (Enumerable, etc.)
using System.Collections.Generic;     // List<T>, HashSet<T>
using System.Data;                    // DataTable, DataRow
using System.Linq;                    // LINQ
using Perfilado.Modelos;              // Record MetricaCombinacion
using Perfilado.Utils;                // HashingUtils.Sha256

namespace Perfilado.Servicios;

public class ServicioCombinaciones
{
    /// <summary>
    /// Genera métricas para las combinaciones de columnas candidatas.
    /// </summary>
    /// <param name="dt">DataTable con la muestra o tabla completa.</param>
    /// <param name="tabla">Nombre de la tabla cruda origen.</param>
    /// <param name="maxK">Máximo tamaño de combinación (1-3).</param>
    /// <param name="columnasCandidatas">
    ///     Columnas que superaron los filtros previos (unicidad, nulos).
    /// </param>
    /// <param name="maxCombinacionesTotales">
    ///     Límite duro para evitar explosión combinatoria.
    /// </param>
    public List<MetricaCombinacion> Generar(
        DataTable dt,
        string tabla,
        int maxK,
        string[] columnasCandidatas,
        int maxCombinacionesTotales)
    {
        var resultado = new List<MetricaCombinacion>();   // salida final
        long totalFilas = dt.Rows.Count;                  // para % unicidad

        // ─────────────────── Bucle por tamaño k (1,2,3) ───────────────────
        foreach (int k in Enumerable.Range(1, maxK))
        {
            // Recorremos cada combinación específica de tamaño k
            foreach (var cols in Combinar(columnasCandidatas, k))
            {
                // Si ya alcanzamos el máximo global, salimos del loop anidado
                if (resultado.Count >= maxCombinacionesTotales) break;

                // Regla de negocio:
                // La combinación debe contener al menos UNA columna “fuerte”
                // (unicidad ≥ 50 %) para no procesar combinaciones débiles.
                if (!ContieneColumnaFuerte(cols)) continue;

                // HashSet para contar valores únicos de la clave concatenada
                var setValores = new HashSet<string>(capacity: (int)totalFilas);

                foreach (DataRow row in dt.Rows)          // recorrido fila-a-fila
                {
                    // Concatenamos los valores de las columnas con separador |
                    var clave = string.Join("|",
                        cols.Select(c => row[c]?.ToString() ?? ""));
                    setValores.Add(clave);                // solo únicos
                }

                long cardinalidad = setValores.Count;     // nº claves distintas
                double porcUnico =                        // % unicidad
                    Math.Round(cardinalidad * 100.0 / totalFilas, 2);

                // Descartamos combinaciones cuyo % unicidad < 30 %
                if (porcUnico < 30) continue;

                // Creamos el record con las métricas y lo añadimos a la lista
                resultado.Add(new MetricaCombinacion(
                    tabla,
                    cols,
                    cardinalidad,
                    porcUnico,
                    HashingUtils.Sha256(string.Join(",", cols))  // fingerprint
                ));
            }
        }
        return resultado;

        /*───────────────────  Función local  ─────────────────────────────*/
        // Comprueba si la combinación incluye ≥ 1 columna con unicidad ≥ 50 %.
        bool ContieneColumnaFuerte(string[] cols) =>
            cols.Any(c =>                                  // al menos una
                dt.AsEnumerable()
                  .Select(r => r[c]?.ToString() ?? "")     // columna c como string
                  .Distinct()
                  .Count() >= 0.5 * totalFilas);           // ≥ 50 % únicas
    }

    // ────────────────── Generador de combinaciones  ──────────────────────
    // Devuelve todas las combinaciones “sin repetición” de 'items'
    // de tamaño exacto k, usando un algoritmo iterativo (sin recursión).
    private static IEnumerable<string[]> Combinar(string[] items, int k)
    {
        int n = items.Length;                  // nº total de columnas
        var idx = Enumerable.Range(0, k).ToArray();  // índices iniciales 0..k-1

        while (true)
        {
            // Yield de la combinación actual
            yield return idx.Select(i => items[i]).ToArray();

            // Buscar el índice t que aún se pueda incrementar
            int t = k - 1;
            while (t >= 0 && idx[t] == n - k + t) t--;
            if (t < 0) break;                  // todas las combinaciones listas

            idx[t]++;                          // incrementamos posición t
            for (int i = t + 1; i < k; i++)    // y reajustamos las siguientes
                idx[i] = idx[i - 1] + 1;
        }
    }
}
