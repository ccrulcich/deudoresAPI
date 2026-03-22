using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Repositories;

public interface IDeudorRepository
{
    Task UpsertRangeAsync(IEnumerable<Deudor> deudores, CancellationToken ct = default);

    Task<Deudor?> GetByIdentificacionAsync(string cuit, CancellationToken ct = default);

    /// <summary>
    /// Retorna los N deudores con mayor suma total de préstamos.
    /// </summary>
    Task<IEnumerable<Deudor>> GetTopAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Retorna deudores con una situación máxima específica, con paginación.
    /// </summary>
    Task<(IEnumerable<Deudor> Items, int TotalCount)> GetBySituacionAsync(
        int situacion, int page = 1, int pageSize = 50, CancellationToken ct = default);
}
