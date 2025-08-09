// Modelos/Dtos/EtiquetaCreateDto.cs
// DTO para crear/upsert. Uso arrays de columnas para que la API sea cómoda.

namespace Etiquetado.Modelos.Dtos;

public class EtiquetaCreateDto
{
    public string   TablaA      { get; set; } = default!;
    public string[] ColumnasA   { get; set; } = default!;
    public string   TablaB      { get; set; } = default!;
    public string[] ColumnasB   { get; set; } = default!;
    public bool     EsLlave     { get; set; }
    public string?  Justificacion { get; set; }
    public string?  Usuario       { get; set; }
}
