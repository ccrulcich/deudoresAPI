using DeudoresApi.Domain.Models;
using DeudoresApi.Domain.Services;

namespace DeudoresApi.Infrastructure.Parsing;

/// <summary>
/// Implementación concreta del parser BCRA.
/// Vive en Infrastructure porque conoce detalles del formato físico del archivo.
/// Al inyectarse via IBcraParser, Application y Domain no dependen de esta clase.
/// </summary>
public class BcraParser : IBcraParser
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

    public async IAsyncEnumerable<BcraRecord> ParseAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.Length < MinLineLength)
                continue;

            var record = TryParseLine(line);
            if (record != null)
                yield return record;
        }
    }

    public async Task<(List<Deudor> Deudores, List<Entidad> Entidades)> ProcessAsync(Stream fileStream)
    {
        var deudoresDict = new Dictionary<string, (int situacionMax, decimal sumaTotal)>();
        var entidadesDict = new Dictionary<string, decimal>();

        await foreach (var record in ParseAsync(fileStream))
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

    private static BcraRecord? TryParseLine(string line)
    {
        try
        {
            var codigoEntidad = line.Substring(PosCodigoEntidad, LenCodigoEntidad).Trim();
            var nroIdentificacion = line.Substring(PosNroIdentificacion, LenNroIdentificacion).Trim();
            var situacionRaw = line.Substring(PosSituacion, LenSituacion).Trim();
            var prestamosRaw = line.Substring(PosPrestamos, LenPrestamos).Trim();

            if (string.IsNullOrEmpty(codigoEntidad) || string.IsNullOrEmpty(nroIdentificacion))
                return null;

            if (!int.TryParse(situacionRaw, out var situacion))
                return null;

            if (!decimal.TryParse(
                    prestamosRaw.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var prestamos))
                prestamos = 0;

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
        catch
        {
            return null;
        }
    }
}
