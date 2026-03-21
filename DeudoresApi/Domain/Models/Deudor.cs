namespace DeudoresApi.Domain.Models;

// Modelo de dominio puro — sin anotaciones de infraestructura.
// EF Core se configura exclusivamente via Fluent API en AppDbContext.
public class Deudor
{
    public string NroIdentificacion { get; set; } = string.Empty;
    public int SituacionMaxima { get; set; }
    public decimal SumaTotalPrestamos { get; set; }
}
