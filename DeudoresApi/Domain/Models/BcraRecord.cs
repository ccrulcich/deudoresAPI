namespace DeudoresApi.Domain.Models;

public class BcraRecord
{
    public string CodigoEntidad { get; set; } = string.Empty;
    public string FechaInformacion { get; set; } = string.Empty;
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Actividad { get; set; } = string.Empty;
    public int Situacion { get; set; }
    public decimal Prestamos { get; set; }
}
