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
    public async Task<DeudorDto?> GetDeudorAsync(string cuit, CancellationToken ct = default)
    {
        var deudor = await deudorRepo.GetByIdentificacionAsync(cuit, ct);
        if (deudor is null) return null;

        return new DeudorDto(deudor.Cuit, deudor.SituacionMaxima, deudor.SumaTotalPrestamos);
    }

    public async Task<EntidadDto?> GetEntidadAsync(string codigoEntidad, CancellationToken ct = default)
    {
        var entidad = await entidadRepo.GetByCodigoAsync(codigoEntidad, ct);
        if (entidad is null) return null;

        return new EntidadDto(entidad.CodigoEntidad, entidad.SumaTotalPrestamos);
    }

    public async Task<IEnumerable<DeudorDto>> GetTopDeudoresAsync(int count, CancellationToken ct = default)
    {
        var deudores = await deudorRepo.GetTopAsync(count, ct);

        return deudores.Select(d =>
            new DeudorDto(d.Cuit, d.SituacionMaxima, d.SumaTotalPrestamos));
    }

    public async Task<PagedResultDto<DeudorDto>> GetDeudoresBySituacionAsync(
        int situacion, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var (items, totalCount) = await deudorRepo.GetBySituacionAsync(situacion, page, pageSize, ct);

        var dtos = items.Select(d =>
            new DeudorDto(d.Cuit, d.SituacionMaxima, d.SumaTotalPrestamos));

        return new PagedResultDto<DeudorDto>(dtos, totalCount, page, pageSize);
    }
}
