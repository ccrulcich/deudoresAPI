using DeudoresApi.Infrastructure.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DeudoresApi.Tests;

/// <summary>
/// Tests unitarios para BcraParser.
/// Cada test construye un stream en memoria con líneas de formato fijo BCRA,
/// sin necesidad de archivos en disco ni base de datos.
/// </summary>
public class BcraParserTests
{
    // Formato BCRA (longitud fija):
    // Pos  0-4   (5):  CodigoEntidad
    // Pos  5-10  (6):  FechaInformacion
    // Pos 11-12  (2):  TipoIdentificacion
    // Pos 13-23  (11): NroIdentificacion
    // Pos 24-26  (3):  Actividad
    // Pos 27-28  (2):  Situacion
    // Pos 29-40  (12): Prestamos
    // Total mínimo: 41 caracteres

    private static BcraParser CreateParser() =>
        new(NullLogger<BcraParser>.Instance);

    private static Stream ToStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    // Crea una línea válida con los valores indicados, rellenando con espacios.
    private static string MakeLine(
        string entidad = "00001",
        string fecha = "202501",
        string tipo = "20",
        string nroId = "20123456781",
        string actividad = "001",
        string situacion = " 1",
        string prestamos = "000000010000") =>
        $"{entidad,-5}{fecha,-6}{tipo,-2}{nroId,-11}{actividad,-3}{situacion,-2}{prestamos,-12}";

    // ─── ParseAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_LineaValida_RetornaUnRegistro()
    {
        var parser = CreateParser();
        var line = MakeLine();
        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();

        await foreach (var r in parser.ParseAsync(ToStream(line)))
            records.Add(r);

        Assert.Single(records);
        Assert.Equal("00001", records[0].CodigoEntidad);
        Assert.Equal("20123456781", records[0].NroIdentificacion);
        Assert.Equal(1, records[0].Situacion);
        Assert.Equal(10000m, records[0].Prestamos);
    }

    [Fact]
    public async Task ParseAsync_VariasLineasValidas_RetornaTodosLosRegistros()
    {
        var parser = CreateParser();
        var content = string.Join("\n",
            MakeLine(entidad: "00001", nroId: "20111111111", situacion: " 1", prestamos: "000000001000"),
            MakeLine(entidad: "00002", nroId: "20222222222", situacion: " 3", prestamos: "000000005000")
        );

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream(content)))
            records.Add(r);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task ParseAsync_LineaDemasiadoCorta_EsIgnorada()
    {
        var parser = CreateParser();
        var content = "00001202501"; // línea de solo 11 chars, menor al mínimo de 41

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream(content)))
            records.Add(r);

        Assert.Empty(records);
    }

    [Fact]
    public async Task ParseAsync_CamposObligatoriosVacios_EsIgnorada()
    {
        var parser = CreateParser();
        // CodigoEntidad vacío
        var line = MakeLine(entidad: "     ");

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream(line)))
            records.Add(r);

        Assert.Empty(records);
    }

    [Fact]
    public async Task ParseAsync_SituacionNoNumerica_EsIgnorada()
    {
        var parser = CreateParser();
        var line = MakeLine(situacion: "XX");

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream(line)))
            records.Add(r);

        Assert.Empty(records);
    }

    [Fact]
    public async Task ParseAsync_PrestamosNoParseable_UsaCeroYContinua()
    {
        var parser = CreateParser();
        var line = MakeLine(prestamos: "INVALIDO    ");

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream(line)))
            records.Add(r);

        // El registro se incluye con prestamos = 0
        Assert.Single(records);
        Assert.Equal(0m, records[0].Prestamos);
    }

    [Fact]
    public async Task ParseAsync_StreamVacio_NoRetornaRegistros()
    {
        var parser = CreateParser();

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream("")))
            records.Add(r);

        Assert.Empty(records);
    }

    [Fact]
    public async Task ParseAsync_MixDeLineasValidasEInvalidas_RetornaSoloValidas()
    {
        var parser = CreateParser();
        var content = string.Join("\n",
            "corta",                                                              // muy corta
            MakeLine(nroId: "20333333333", situacion: " 2", prestamos: "000000002000"), // válida
            MakeLine(situacion: "AB"),                                            // situación inválida
            MakeLine(nroId: "20444444444", situacion: " 5", prestamos: "000000008000")  // válida
        );

        var records = new List<DeudoresApi.Domain.Models.BcraRecord>();
        await foreach (var r in parser.ParseAsync(ToStream(content)))
            records.Add(r);

        Assert.Equal(2, records.Count);
    }

    // ─── ProcessAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_UnDeudorUnaEntidad_RetornaCorrecto()
    {
        var parser = CreateParser();
        var line = MakeLine(entidad: "00001", nroId: "20123456781", situacion: " 2", prestamos: "000000005000");

        var (deudores, entidades) = await parser.ProcessAsync(ToStream(line));

        Assert.Single(deudores);
        Assert.Equal("20123456781", deudores[0].NroIdentificacion);
        Assert.Equal(2, deudores[0].SituacionMaxima);
        Assert.Equal(5000m, deudores[0].SumaTotalPrestamos);

        Assert.Single(entidades);
        Assert.Equal("00001", entidades[0].CodigoEntidad);
        Assert.Equal(5000m, entidades[0].SumaTotalPrestamos);
    }

    [Fact]
    public async Task ProcessAsync_MismoDeudorEnDosEntidades_AcumulaPrestamosYTomaSituacionMaxima()
    {
        var parser = CreateParser();
        var content = string.Join("\n",
            MakeLine(entidad: "00001", nroId: "20111111111", situacion: " 1", prestamos: "000000003000"),
            MakeLine(entidad: "00002", nroId: "20111111111", situacion: " 4", prestamos: "000000007000")
        );

        var (deudores, entidades) = await parser.ProcessAsync(ToStream(content));

        var deudor = Assert.Single(deudores);
        Assert.Equal(4, deudor.SituacionMaxima);        // máximo entre 1 y 4
        Assert.Equal(10000m, deudor.SumaTotalPrestamos); // 3000 + 7000

        Assert.Equal(2, entidades.Count);
    }

    [Fact]
    public async Task ProcessAsync_MismaEntidadVariosDeudores_AcumulaPrestamosPorEntidad()
    {
        var parser = CreateParser();
        var content = string.Join("\n",
            MakeLine(entidad: "00001", nroId: "20111111111", situacion: " 1", prestamos: "000000004000"),
            MakeLine(entidad: "00001", nroId: "20222222222", situacion: " 2", prestamos: "000000006000")
        );

        var (deudores, entidades) = await parser.ProcessAsync(ToStream(content));

        Assert.Equal(2, deudores.Count);

        var entidad = Assert.Single(entidades);
        Assert.Equal("00001", entidad.CodigoEntidad);
        Assert.Equal(10000m, entidad.SumaTotalPrestamos); // 4000 + 6000
    }

    [Fact]
    public async Task ProcessAsync_ArchivoVacio_RetornaListasVacias()
    {
        var parser = CreateParser();

        var (deudores, entidades) = await parser.ProcessAsync(ToStream(""));

        Assert.Empty(deudores);
        Assert.Empty(entidades);
    }
}
