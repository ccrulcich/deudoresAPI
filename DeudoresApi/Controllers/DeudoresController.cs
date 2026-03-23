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
    /// Opcionalmente filtra por situación máxima (1–6).
    /// </summary>
    [HttpGet("top/{n:int}")]
    public async Task<IActionResult> GetTop(int n, [FromQuery] int? situacion, CancellationToken ct)
    {
        if (n <= 0 || n > 1000)
            return BadRequest("El valor de N debe ser entre 1 y 1000.");

        if (situacion.HasValue && (situacion < 1 || situacion > 6))
            return BadRequest("El valor de 'situacion' debe ser entre 1 y 6.");

        var top = await queryService.GetTopDeudoresAsync(n, situacion, ct);
        return Ok(top);
    }
}
