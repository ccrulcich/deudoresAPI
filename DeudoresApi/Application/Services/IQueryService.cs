using DeudoresApi.Application.DTOs;

namespace DeudoresApi.Application.Services;

/// <summary>
/// Contrato para consultas de datos ya procesados.
/// Separado de IImportService para respetar el principio de responsabilidad única:
/// importar y consultar son casos de uso distintos.
/// </summary>
public interface IQueryService
{
    Task<DeudorDto?> GetDeudorAsync(string nroIdentificacion);
    Task<EntidadDto?> GetEntidadAsync(string codigoEntidad);

    /// <summary>
    /// Top N deudores por suma total de préstamos, descendente.
    /// </summary>
    Task<IEnumerable<DeudorDto>> GetTopDeudoresAsync(int count);

    /// <summary>
    /// Retorna todos los deudores con una situación máxima específica.
    /// </summary>
    Task<IEnumerable<DeudorDto>> GetDeudoresBySituacionAsync(int situacion);
}
