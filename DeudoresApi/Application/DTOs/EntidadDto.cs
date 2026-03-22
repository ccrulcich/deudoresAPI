namespace DeudoresApi.Application.DTOs;

/// <summary>
/// DTO de respuesta para una entidad financiera.
/// </summary>
public record EntidadDto(
    string CodigoEntidad,
    decimal SumaTotalPrestamos,
    string Nombre = "Nombre no brindado por ex entidad financiera");
