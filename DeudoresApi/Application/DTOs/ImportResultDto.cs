namespace DeudoresApi.Application.DTOs;

/// <summary>
/// DTO de respuesta del proceso de importación.
/// Desacopla el contrato HTTP del modelo de dominio interno.
/// </summary>
public record ImportResultDto(int DeudoresCount, int EntidadesCount);
