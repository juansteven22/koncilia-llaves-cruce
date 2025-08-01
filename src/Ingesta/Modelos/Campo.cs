// Modelos/Campo.cs
namespace Ingesta.Modelos;

public record Campo(
    string fName,
    int    fOffset,
    int    fLength,
    string? caption = null,
    string? fFormat = null,
    bool   Redefines = false
);
