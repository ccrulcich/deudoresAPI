using DeudoresApi.Application.DTOs;
using DeudoresApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportController(IImportService importService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Archivo inválido");

        using var stream = file.OpenReadStream();
        var result = await importService.ProcessAsync(stream);

        return Ok(ToResponse(result));
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
        var result = await importService.ProcessAsync(stream);

        return Ok(ToResponse(result));
    }

    private static object ToResponse(ImportResultDto result) => new
    {
        message = "Archivo procesado y persistido",
        deudores = result.DeudoresCount,
        entidades = result.EntidadesCount
    };
}

public record ProcessLocalRequest(string FilePath);