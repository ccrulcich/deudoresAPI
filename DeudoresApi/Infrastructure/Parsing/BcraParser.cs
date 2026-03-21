using DeudoresApi.Domain.Models;
using DeudoresApi.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace DeudoresApi.Infrastructure.Parsing;

/// <summary>
/// Implementación concreta del parser BCRA.
/// Vive en Infrastructure porque conoce detalles del formato físico del archivo.
/// Al inyectarse via IBcraParser, Application y Domain no dependen de esta clase.
/// </summary>
public class BcraParser(ILogger<BcraParser> logger) : IBcraParser
{
    private const int PosCodigoEntidad = 0;
    private const int LenCodigoEntidad = 5;
    private const int PosFechaInformacion = 5;
    private const int LenFechaInformacion = 6;
    private const int PosTipoIdentificacion = 11;
    private const int LenTipoIdentificacion = 2;
    private const int PosNroIdentificacion = 13;
    private const int LenNroIdentificacion = 11;
    private const int PosActividad = 24;
    private const int LenActividad = 3;
    private const int PosSituacion = 27;
    private const int LenSituacion = 2;
    private const int PosPrestamos = 29;
    private const int LenPrestamos = 12;
    private const int MinLineLength = PosPrestamos + LenPrestamos;

    public async IAsyncEnumerable<BcraRecord> ParseAsync(
        Stream fileStream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream);

        string? line;
        var lineNumber = 0;

        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            ct.ThrowIfCancellationRequested();
            lineNumber++;

            if (line.Length < MinLineLength)
            {
                logger.LogWarning(
                    "Línea {LineNumber} ignorada: longitud {Length} menor al mínimo {MinLength}",
                    lineNumber, line.Length, MinLineLength);
                continue;
            }

            var record = TryParseLine(line, lineNumber);
            if (record != null)
                yield return record;
        }
    }

    public async Task<(List<Deudor> Deudores, List<Entidad> Entidades)> ProcessAsync(
        Stream fileStream,
        CancellationToken ct = default)
    {
        var deudoresDict = new Dictionary<string, (int situacionMax, decimal sumaTotal)>();
        var entidadesDict = new Dictionary<string, decimal>();

        await foreach (var record in ParseAsync(fileStream, ct))
        {
            if (deudoresDict.TryGetValue(record.NroIdentificacion, out var existing))
            {
                deudoresDict[record.NroIdentificacion] = (
                    Math.Max(existing.situacionMax, record.Situacion),
                    existing.sumaTotal + record.Prestamos);
            }
            else
            {
                deudoresDict[record.NroIdentificacion] = (record.Situacion, record.Prestamos);
            }

            entidadesDict.TryGetValue(record.CodigoEntidad, out var entidadTotal);
            entidadesDict[record.CodigoEntidad] = entidadTotal + record.Prestamos;
        }

        var deudores = deudoresDict.Select(kv => new Deudor
        {
            NroIdentificacion = kv.Key,
            SituacionMaxima = kv.Value.situacionMax,
            SumaTotalPrestamos = kv.Value.sumaTotal
        }).ToList();

        var entidades = entidadesDict.Select(kv => new Entidad
        {
            CodigoEntidad = kv.Key,
            SumaTotalPrestamos = kv.Value
        }).ToList();

        return (deudores, entidades);
    }

    private BcraRecord? TryParseLine(string line, int lineNumber)
    {
        try
        {
            var codigoEntidad = line.Substring(PosCodigoEntidad, LenCodigoEntidad).Trim();
            var nroIdentificacion = line.Substring(PosNroIdentificacion, LenNroIdentificacion).Trim();
            var situacionRaw = line.Substring(PosSituacion, LenSituacion).Trim();
            var prestamosRaw = line.Substring(PosPrestamos, LenPrestamos).Trim();

            if (string.IsNullOrEmpty(codigoEntidad) || string.IsNullOrEmpty(nroIdentificacion))
            {
                logger.LogWarning(
                    "Línea {LineNumber}: campos obligatorios vacíos (entidad='{Entidad}', id='{Id}')",
                    lineNumber, codigoEntidad, nroIdentificacion);
                return null;
            }

            if (!int.TryParse(situacionRaw, out var situacion))
            {
                logger.LogWarning(
                    "Línea {LineNumber}: situación '{SituacionRaw}' no es un entero válido",
                    lineNumber, situacionRaw);
                return null;
            }

            if (!decimal.TryParse(
                    prestamosRaw.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var prestamos))
            {
                logger.LogWarning(
                    "Línea {LineNumber}: préstamos '{PrestamosRaw}' no pudo parsearse, se usará 0",
                    lineNumber, prestamosRaw);
                prestamos = 0;
            }

            return new BcraRecord
            {
                CodigoEntidad = codigoEntidad,
                FechaInformacion = line.Substring(PosFechaInformacion, LenFechaInformacion).Trim(),
                TipoIdentificacion = line.Substring(PosTipoIdentificacion, LenTipoIdentificacion).Trim(),
                NroIdentificacion = nroIdentificacion,
                Actividad = line.Substring(PosActividad, LenActividad).Trim(),
                Situacion = situacion,
                Prestamos = prestamos
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al parsear línea {LineNumber}", lineNumber);
            return null;
        }
    }
}

