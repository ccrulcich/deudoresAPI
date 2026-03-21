namespace DeudoresApi.Models;

public class Deudor
{
    public string NroIdentificacion { get; set; } = string.Empty;
    public int SituacionMaxima { get; set; }
    public decimal SumaTotalPrestamos { get; set; }
}
