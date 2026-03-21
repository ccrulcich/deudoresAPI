using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Repositories;

public interface IDeudorRepository
{
    Task UpsertRangeAsync(IEnumerable<Deudor> deudores);
}
