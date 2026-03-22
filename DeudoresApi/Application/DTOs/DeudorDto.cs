namespace DeudoresApi.Application.DTOs;

/// <summary>
/// DTO de respuesta para un deudor.
/// Expone solo lo que el contrato HTTP necesita,
/// sin acoplar al consumidor con el modelo de dominio interno.
/// </summary>
public record DeudorDto(
    string Cuit,
    int SituacionMaxima,
    decimal SumaTotalPrestamos,
    string Nombre = "Nombre no brindado por ex entidad financiera");
