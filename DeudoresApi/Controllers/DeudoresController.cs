using DeudoresApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DeudoresController(IQueryService queryService) : ControllerBase
{
    /// <summary>
    /// Retorna un deudor por su número de identificación (CUIT/CUIL).
    /// </summary>
    [HttpGet("{cuit}")]
    public async Task<IActionResult> GetByIdentificacion(string cuit, CancellationToken ct)
    {
        var deudor = await queryService.GetDeudorAsync(cuit, ct);

        if (deudor is null)
            return NotFound($"No se encontró deudor con identificación: {cuit}");

        return Ok(deudor);
    }

    /// <summary>
    /// Retorna los N deudores con mayor suma total de préstamos.
    /// </summary>
    [HttpGet("top/{n:int}")]
    public async Task<IActionResult> GetTop(int n, CancellationToken ct)
    {
        if (n <= 0 || n > 1000)
            return BadRequest("El valor de N debe ser entre 1 y 1000.");

        var top = await queryService.GetTopDeudoresAsync(n, ct);
        return Ok(top);
    }

    /// <summary>
    /// Retorna deudores con una situación máxima específica (1–6), con paginación.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBySituacion(
        [FromQuery] int? situacion,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (situacion is null)
            return BadRequest("El parámetro 'situacion' es requerido.");

        if (situacion < 1 || situacion > 6)
            return BadRequest("El valor de 'situacion' debe ser entre 1 y 6.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await queryService.GetDeudoresBySituacionAsync(situacion.Value, page, pageSize, ct);
        return Ok(result);
    }
}
