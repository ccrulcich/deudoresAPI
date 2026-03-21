using DeudoresApi.Application.DTOs;

namespace DeudoresApi.Application.Services;

public interface IImportService
{
    Task<ImportResultDto> ProcessAsync(Stream fileStream);
}
