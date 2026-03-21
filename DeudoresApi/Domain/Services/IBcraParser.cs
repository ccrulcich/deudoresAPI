using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Services;

/// <summary>
/// Contrato para parsear archivos BCRA de longitud fija.
/// Definido en Domain para que Application dependa de la abstracción,
/// no de la implementación concreta (que vive en Infrastructure).
/// </summary>
public interface IBcraParser
{
    IAsyncEnumerable<BcraRecord> ParseAsync(Stream fileStream);
    Task<(List<Deudor> Deudores, List<Entidad> Entidades)> ProcessAsync(Stream fileStream);
}
