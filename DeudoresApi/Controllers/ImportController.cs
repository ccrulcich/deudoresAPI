using DeudoresApi.Application.DTOs;
using DeudoresApi.Application.Services;
using DeudoresApi.Domain.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportController(
    IImportService importService,
    IConfiguration configuration,
    IImportQueue? importQueue = null) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Archivo invlido");

        var validationError = ValidateFile(file.FileName, file.Length);
        if (validationError is not null)
            return BadRequest(validationError);

        // Si hay cola SQS configurada, guardamos el archivo y encolamos (async)
        if (importQueue is not null)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{file.FileName}");
            await using (var fs = System.IO.File.Create(tempPath))
                await file.CopyToAsync(fs);

            await importQueue.EnqueueAsync(tempPath);

            return Accepted(new { message = "Archivo encolado para procesamiento ascrono", archivo = file.FileName });
        }

        // Sin cola: procesamiento scrono (modo default)
        using var stream = file.OpenReadStream();
        var result = await importService.ProcessAsync(stream);
        return Ok(ToResponse(result));
    }

    /// <summary>
    /// Procesa un archivo desde una ruta local del servidor (til para archivos grandes).
    /// </summary>
    [HttpPost("process-local")]
    public async Task<IActionResult> ProcessLocal([FromBody] ProcessLocalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest("La ruta del archivo es requerida");

        if (!System.IO.File.Exists(request.FilePath))
            return NotFound($"Archivo no encontrado: {request.FilePath}");

        var fileInfo = new FileInfo(request.FilePath);
        var validationError = ValidateFile(fileInfo.Name, fileInfo.Length);
        if (validationError is not null)
            return BadRequest(validationError);

        // Si hay cola SQS configurada, encolamos directamente la ruta
        if (importQueue is not null)
        {
            await importQueue.EnqueueAsync(request.FilePath);
            return Accepted(new { message = "Archivo encolado para procesamiento ascrono", archivo = request.FilePath });
        }

        using var stream = System.IO.File.OpenRead(request.FilePath);
        var result = await importService.ProcessAsync(stream);
        return Ok(ToResponse(result));
    }

    private string? ValidateFile(string fileName, long fileSize)
    {
        var allowedExtensions = configuration
            .GetSection("FileUpload:AllowedExtensions")
            .Get<string[]>() ?? [".txt"];

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return $"Extensinnn '{ext}' no permitida. Se aceptan: {string.Join(", ", allowedExtensions)}";

        var maxSizeMb = configuration.GetValue<int>("FileUpload:MaxFileSizeMb", 100);
        var maxSizeBytes = (long)maxSizeMb * 1024 * 1024;
        if (fileSize > maxSizeBytes)
            return $"El archivo supera el tamao mximo permitido de {maxSizeMb} MB";

        return null;
    }

    private static object ToResponse(ImportResultDto result) => new
    {
        message = "Archivo procesado y persistido",
        deudores = result.DeudoresCount,
        entidades = result.EntidadesCount
    };
}

public record ProcessLocalRequest(string FilePath);
