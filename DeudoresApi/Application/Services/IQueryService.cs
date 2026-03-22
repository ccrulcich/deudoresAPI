using DeudoresApi.Application.DTOs;

namespace DeudoresApi.Application.Services;

/// <summary>
/// Contrato para consultas de datos ya procesados.
/// Separado de IImportService para respetar el principio de responsabilidad única:
/// importar y consultar son casos de uso distintos.
/// </summary>
public interface IQueryService
{
    Task<DeudorDto?> GetDeudorAsync(string cuit, CancellationToken ct = default);
    Task<EntidadDto?> GetEntidadAsync(string codigoEntidad, CancellationToken ct = default);

    /// <summary>
    /// Top N deudores por suma total de préstamos, descendente.
    /// </summary>
    Task<IEnumerable<DeudorDto>> GetTopDeudoresAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Retorna deudores con una situación máxima específica, con paginación.
    /// </summary>
    Task<PagedResultDto<DeudorDto>> GetDeudoresBySituacionAsync(
        int situacion, int page = 1, int pageSize = 50, CancellationToken ct = default);
}
