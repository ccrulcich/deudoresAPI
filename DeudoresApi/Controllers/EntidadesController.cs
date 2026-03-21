using DeudoresApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EntidadesController(IQueryService queryService) : ControllerBase
{
    /// <summary>
    /// Retorna una entidad financiera por su código BCRA.
    /// </summary>
    [HttpGet("{codigo}")]
    public async Task<IActionResult> GetByCodigo(string codigo, CancellationToken ct)
    {
        var entidad = await queryService.GetEntidadAsync(codigo, ct);

        if (entidad is null)
            return NotFound($"No se encontró entidad con código: {codigo}");

        return Ok(entidad);
    }
}
