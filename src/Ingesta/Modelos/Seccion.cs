
// Modelos/Seccion.cs
namespace Ingesta.Modelos;

public record Seccion(
    string Name,
    string? Filter,
    List<Campo> Fields
);
