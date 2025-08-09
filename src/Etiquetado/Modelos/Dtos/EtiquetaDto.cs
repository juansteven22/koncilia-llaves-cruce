// Modelos/Dtos/EtiquetaDto.cs
// DTO de salida. Entrego columnas como array para que el cliente no parsee CSV.

namespace Etiquetado.Modelos.Dtos;

public class EtiquetaDto
{
    public int      Id           { get; set; }
    public string   TablaA       { get; set; } = default!;
    public string[] ColumnasA    { get; set; } = default!;
    public string   TablaB       { get; set; } = default!;
    public string[] ColumnasB    { get; set; } = default!;
    public bool     EsLlave      { get; set; }
    public string?  Justificacion{ get; set; }
    public string?  Usuario      { get; set; }
    public string   HashEtiqueta { get; set; } = default!;
    public string   FechaCreacion{ get; set; } = default!;
    public string?  FechaActualizacion { get; set; }
}
