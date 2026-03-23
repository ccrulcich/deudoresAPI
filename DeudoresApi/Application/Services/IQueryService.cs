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
    /// Opcionalmente filtra por situación máxima.
    /// </summary>
    Task<IEnumerable<DeudorDto>> GetTopDeudoresAsync(int count, int? situacion = null, CancellationToken ct = default);
}
