// Modelos/EtiquetaLlave.cs
// Esta clase representa el registro tal como vive en la base de datos.
// En primera persona: aquí incluyo también el HashEtiqueta para idempotencia.

using System;

namespace Etiquetado.Modelos;

public class EtiquetaLlave
{
    public int      Id                 { get; set; }
    public string   HashEtiqueta       { get; set; } = default!; // SHA-256 del par
    public string   TablaA             { get; set; } = default!;
    public string   ColumnasA          { get; set; } = default!; // CSV normalizado
    public string   TablaB             { get; set; } = default!;
    public string   ColumnasB          { get; set; } = default!; // CSV normalizado
    public bool     EsLlave            { get; set; }             // true = llave válida
    public string?  Justificacion      { get; set; }
    public string?  Usuario            { get; set; }
    public DateTime FechaCreacion      { get; set; }
    public DateTime?FechaActualizacion { get; set; }
}
