using DeudoresApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportController : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Archivo inválido");

        using var stream = file.OpenReadStream();

        var (deudores, entidades) = await BcraParser.ProcessAsync(stream);

        return Ok(new
        {
            message = "Archivo procesado",
            deudores = deudores.Count,
            entidades = entidades.Count
        });
    }

    /// <summary>
    /// Procesa un archivo desde una ruta local del servidor (útil para archivos grandes).
    /// </summary>
    [HttpPost("process-local")]
    public async Task<IActionResult> ProcessLocal([FromBody] ProcessLocalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest("La ruta del archivo es requerida");

        if (!System.IO.File.Exists(request.FilePath))
            return NotFound($"Archivo no encontrado: {request.FilePath}");

        using var stream = System.IO.File.OpenRead(request.FilePath);

        var (deudores, entidades) = await BcraParser.ProcessAsync(stream);

        return Ok(new
        {
            message = "Archivo procesado",
            deudores = deudores.Count,
            entidades = entidades.Count
        });
    }
}

public record ProcessLocalRequest(string FilePath);