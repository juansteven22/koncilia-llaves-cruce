

// Modelos/EsquemaArchivo.cs
namespace Ingesta.Modelos;

public record EsquemaArchivo(
    string type,                // "fixed" | "csv"
    bool   header,
    int?   length,
    int?   decimals,
    string? decimalPoint,
    string? separator,
    List<Seccion> Sections
);
