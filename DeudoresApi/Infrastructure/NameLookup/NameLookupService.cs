using DeudoresApi.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeudoresApi.Infrastructure.NameLookup;

/// <summary>
/// Carga los archivos Nomdeu.txt (nombres de deudores) y Maeent.txt (nombres de entidades)
/// en diccionarios en memoria al iniciar. Si los archivos no existen, devuelve null sin fallar.
/// Las rutas se configuran via NameLookup:DeudoresFile y NameLookup:EntidadesFile en appsettings.
/// </summary>
public class NameLookupService : INameLookupService
{
    private readonly IReadOnlyDictionary<string, string> _deudores;
    private readonly IReadOnlyDictionary<string, string> _entidades;

    public NameLookupService(IConfiguration configuration, ILogger<NameLookupService> logger)
    {
        var deudoresFile = configuration["NameLookup:DeudoresFile"] ?? "/data/Nomdeu.txt";
        var entidadesFile = configuration["NameLookup:EntidadesFile"] ?? "/data/Maeent.txt";

        _deudores = LoadFile(deudoresFile, keyLength: 11, logger, "deudores");
        _entidades = LoadFile(entidadesFile, keyLength: 5, logger, "entidades");
    }

    private const string NombreDesconocido = "Nombre no brindado por ex entidad financiera";

    public string GetDeudorNombre(string cuit) =>
        _deudores.TryGetValue(cuit, out var nombre) ? nombre : NombreDesconocido;

    public string GetEntidadNombre(string codigoEntidad) =>
        _entidades.TryGetValue(codigoEntidad, out var nombre) ? nombre : NombreDesconocido;

    private static IReadOnlyDictionary<string, string> LoadFile(
        string path, int keyLength, ILogger logger, string label)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning(
                "Archivo de nombres '{Label}' no encontrado en {Path}. El campo nombre será null.",
                label, path);
            return new Dictionary<string, string>();
        }

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(path))
        {
            if (line.Length <= keyLength) continue;
            var key = line[..keyLength].Trim();
            var nombre = line[keyLength..].Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(nombre))
                dict[key] = nombre;
        }

        logger.LogInformation(
            "Nombres {Label} cargados: {Count} registros desde {Path}",
            label, dict.Count, path);

        return dict;
    }
}
