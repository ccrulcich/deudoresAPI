using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Repositories;

public interface IEntidadRepository
{
    Task UpsertRangeAsync(IEnumerable<Entidad> entidades, CancellationToken ct = default);

    Task<Entidad?> GetByCodigoAsync(string codigoEntidad, CancellationToken ct = default);
}
