using DeudoresApi.Domain.Models;

namespace DeudoresApi.Domain.Repositories;

public interface IDeudorRepository
{
    Task UpsertRangeAsync(IEnumerable<Deudor> deudores);

    Task<Deudor?> GetByIdentificacionAsync(string nroIdentificacion);

    /// <summary>
    /// Retorna los N deudores con mayor suma total de préstamos.
    /// </summary>
    Task<IEnumerable<Deudor>> GetTopAsync(int count);

    /// <summary>
    /// Retorna todos los deudores con una situación máxima específica.
    /// </summary>
    Task<IEnumerable<Deudor>> GetBySituacionAsync(int situacion);
}
