using DeudoresApi.Application.DTOs;
using DeudoresApi.Domain.Repositories;

namespace DeudoresApi.Application.Services;

/// <summary>
/// Implementa los casos de uso de consulta.
/// Usa los repositorios para obtener datos y los proyecta a DTOs.
/// El mapeo Domain → DTO vive aquí (en Application), nunca en el controller ni en el repo.
/// </summary>
public class QueryService(IDeudorRepository deudorRepo, IEntidadRepository entidadRepo) : IQueryService
{
    public async Task<DeudorDto?> GetDeudorAsync(string nroIdentificacion)
    {
        var deudor = await deudorRepo.GetByIdentificacionAsync(nroIdentificacion);
        if (deudor is null) return null;

        return new DeudorDto(deudor.NroIdentificacion, deudor.SituacionMaxima, deudor.SumaTotalPrestamos);
    }

    public async Task<EntidadDto?> GetEntidadAsync(string codigoEntidad)
    {
        var entidad = await entidadRepo.GetByCodigoAsync(codigoEntidad);
        if (entidad is null) return null;

        return new EntidadDto(entidad.CodigoEntidad, entidad.SumaTotalPrestamos);
    }

    public async Task<IEnumerable<DeudorDto>> GetTopDeudoresAsync(int count)
    {
        var deudores = await deudorRepo.GetTopAsync(count);

        return deudores.Select(d =>
            new DeudorDto(d.NroIdentificacion, d.SituacionMaxima, d.SumaTotalPrestamos));
    }

    public async Task<IEnumerable<DeudorDto>> GetDeudoresBySituacionAsync(int situacion)
    {
        var deudores = await deudorRepo.GetBySituacionAsync(situacion);

        return deudores.Select(d =>
            new DeudorDto(d.NroIdentificacion, d.SituacionMaxima, d.SumaTotalPrestamos));
    }
}
