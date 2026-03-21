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
    [HttpGet("{nroIdentificacion}")]
    public async Task<IActionResult> GetByIdentificacion(string nroIdentificacion)
    {
        var deudor = await queryService.GetDeudorAsync(nroIdentificacion);

        // 404 explícito: es un dato esperado que puede no existir, no un error del servidor.
        if (deudor is null)
            return NotFound($"No se encontró deudor con identificación: {nroIdentificacion}");

        return Ok(deudor);
    }

    /// <summary>
    /// Retorna los N deudores con mayor suma total de préstamos.
    /// </summary>
    [HttpGet("top/{n:int}")]
    public async Task<IActionResult> GetTop(int n)
    {
        // Validación de negocio: n debe ser positivo y razonable.
        if (n <= 0 || n > 1000)
            return BadRequest("El valor de N debe ser entre 1 y 1000.");

        var top = await queryService.GetTopDeudoresAsync(n);
        return Ok(top);
    }
}
