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
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Archivo inválido");

        var validationError = ValidateFile(file.FileName, file.Length);
        if (validationError is not null)
            return BadRequest(validationError);

        if (importQueue is not null)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{file.FileName}");
            await using (var fs = System.IO.File.Create(tempPath))
                await file.CopyToAsync(fs, ct);

            await importQueue.EnqueueAsync(tempPath, ct);

            return Accepted(new { message = "Archivo encolado para procesamiento asíncrono", archivo = file.FileName });
        }

        using var stream = file.OpenReadStream();
        var result = await importService.ProcessAsync(stream, ct);
        return Ok(ToResponse(result));
    }

    /// <summary>
    /// Procesa un archivo desde una ruta local del servidor (útil para archivos grandes).
    /// No aplica validación de tamaño porque el archivo ya está en el servidor.
    /// </summary>
    [HttpPost("process-local")]
    public async Task<IActionResult> ProcessLocal([FromBody] ProcessLocalRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest("La ruta del archivo es requerida");

        if (!System.IO.File.Exists(request.FilePath))
            return NotFound($"Archivo no encontrado: {request.FilePath}");

        var fileInfo = new FileInfo(request.FilePath);
        var validationError = ValidateFileExtension(fileInfo.Name);
        if (validationError is not null)
            return BadRequest(validationError);

        if (importQueue is not null)
        {
            await importQueue.EnqueueAsync(request.FilePath, ct);
            return Accepted(new { message = "Archivo encolado para procesamiento asíncrono", archivo = request.FilePath });
        }

        using var stream = System.IO.File.OpenRead(request.FilePath);
        var result = await importService.ProcessAsync(stream, ct);
        return Ok(ToResponse(result));
    }

    private string? ValidateFile(string fileName, long fileSize)
    {
        var extError = ValidateFileExtension(fileName);
        if (extError is not null) return extError;

        var maxSizeMb = configuration.GetValue<int>("FileUpload:MaxFileSizeMb", 6000);
        var maxSizeBytes = (long)maxSizeMb * 1024 * 1024;
        if (fileSize > maxSizeBytes)
            return $"El archivo supera el tamaño máximo permitido de {maxSizeMb} MB";

        return null;
    }

    private string? ValidateFileExtension(string fileName)
    {
        var allowedExtensions = configuration
            .GetSection("FileUpload:AllowedExtensions")
            .Get<string[]>() ?? [".txt"];

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return $"Extensión '{ext}' no permitida. Se aceptan: {string.Join(", ", allowedExtensions)}";

        return null;
    }

    private static object ToResponse(ImportResultDto result) => new
    {
        message = "Archivo procesado y persistido",
        deudores = result.DeudoresCount,
        entidades = result.EntidadesCount
    };
}
