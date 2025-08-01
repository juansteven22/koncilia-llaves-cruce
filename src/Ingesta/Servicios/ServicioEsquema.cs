// Lee y deserializa el JSON-esquema
using System.Text.Json;
using Ingesta.Modelos;

public class ServicioEsquema
{
    private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    public EsquemaArchivo Cargar(string rutaJson)
    {
        var json = File.ReadAllText(rutaJson);
        return JsonSerializer.Deserialize<EsquemaArchivo>(json, _opts)
               ?? throw new InvalidOperationException("JSON inválido.");
    }
}
