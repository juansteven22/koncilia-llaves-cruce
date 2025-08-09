// Modelos/Dtos/EtiquetaUpdateDto.cs
// DTO de actualización. Solo permito cambiar campos de decisión y auditoría.

namespace Etiquetado.Modelos.Dtos;

public class EtiquetaUpdateDto
{
    public bool     EsLlave       { get; set; }
    public string?  Justificacion { get; set; }
    public string?  Usuario       { get; set; }
}
