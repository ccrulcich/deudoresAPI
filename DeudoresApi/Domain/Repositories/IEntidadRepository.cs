using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Repositories;

public interface IEntidadRepository
{
    Task UpsertRangeAsync(IEnumerable<Entidad> entidades);
}
