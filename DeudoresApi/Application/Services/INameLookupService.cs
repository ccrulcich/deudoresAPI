namespace DeudoresApi.Application.Services;

/// <summary>
/// Servicio de lookup de nombres a partir de los archivos Nomdeu.txt y Maeent.txt del BCRA.
/// Se registra como singleton y carga los datos en memoria al iniciar.
/// </summary>
public interface INameLookupService
{
    string GetDeudorNombre(string cuit);
    string GetEntidadNombre(string codigoEntidad);
}
