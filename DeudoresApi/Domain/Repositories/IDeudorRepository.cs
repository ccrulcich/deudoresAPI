using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Repositories;

public interface IDeudorRepository
{
    Task UpsertRangeAsync(IEnumerable<Deudor> deudores, CancellationToken ct = default);

    Task<Deudor?> GetByIdentificacionAsync(string cuit, CancellationToken ct = default);

    /// <summary>
    /// Retorna los N deudores con mayor suma total de préstamos.
    /// Opcionalmente filtra por situación máxima.
    /// </summary>
    Task<IEnumerable<Deudor>> GetTopAsync(int count, int? situacion = null, CancellationToken ct = default);
}
