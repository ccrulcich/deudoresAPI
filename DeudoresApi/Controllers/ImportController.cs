using DeudoresApi.Application.DTOs;
using DeudoresApi.Application.Services;
using DeudoresApi.Domain.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers;

[ApiController]
[Route("/")]
public class ImportController(
    IImportService importService,
    IConfiguration configuration,
    IImportQueue? importQueue = null) : ControllerBase
{
    /// <summary>
    /// Importa un archivo BCRA. Acepta dos modos:
    ///   1. Multipart file upload: enviar el archivo como form-data (campo "file")
    ///   2. Ruta local: enviar form-data con campo "filePath" (string) apuntando al archivo en el servidor
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile? file, [FromForm] string? filePath, CancellationToken ct)
    {
        // Determinar el origen del archivo: upload o ruta local
        if (file is not null && file.Length > 0)
            return await ProcessUploadedFile(file, ct);

        if (!string.IsNullOrWhiteSpace(filePath))
            return await ProcessLocalFile(filePath, ct);

        return BadRequest("Debe enviar un archivo (campo 'file') o una ruta local (campo 'filePath')");
    }

    private async Task<IActionResult> ProcessUploadedFile(IFormFile file, CancellationToken ct)
    {
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

    private async Task<IActionResult> ProcessLocalFile(string filePath, CancellationToken ct)
    {
        if (!System.IO.File.Exists(filePath))
            return NotFound($"Archivo no encontrado: {filePath}");

        var fileInfo = new FileInfo(filePath);
        var validationError = ValidateFileExtension(fileInfo.Name);
        if (validationError is not null)
            return BadRequest(validationError);

        if (importQueue is not null)
        {
            await importQueue.EnqueueAsync(filePath, ct);
            return Accepted(new { message = "Archivo encolado para procesamiento asíncrono", archivo = filePath });
        }

        using var stream = System.IO.File.OpenRead(filePath);
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
