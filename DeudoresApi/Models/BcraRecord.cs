namespace DeudoresApi.Models;

/// <summary>
/// Representa una línea parseada del archivo BCRA de deudores (campos de longitud fija).
/// </summary>
public class BcraRecord
{
    public string CodigoEntidad { get; set; } = string.Empty;   // pos 0, len 5
    public string FechaInformacion { get; set; } = string.Empty; // pos 5, len 6
    public string TipoIdentificacion { get; set; } = string.Empty; // pos 11, len 2
    public string NroIdentificacion { get; set; } = string.Empty;  // pos 13, len 11
    public string Actividad { get; set; } = string.Empty;          // pos 24, len 3
    public int Situacion { get; set; }                              // pos 27, len 2
    public decimal Prestamos { get; set; }                          // pos 29, len 12
}
