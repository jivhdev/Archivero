using Archivero.Datos;
using Archivero.Servicios;

namespace Archivero.Tests;

public class CoincidenciaAutomaticaServiceTests : IDisposable
{
    private readonly string _carpetaTemporal;

    public CoincidenciaAutomaticaServiceTests()
    {
        _carpetaTemporal = Path.Combine(Path.GetTempPath(), "archivero-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_carpetaTemporal);
    }

    private static ConfiguracionDocumento CrearConfiguracion(string emisor, string tipo) => new()
    {
        Id = 1,
        Emisor = emisor,
        Tipo = tipo,
        CarpetaDestino = @"C:\Destino",
        FormatoCarpeta = FormatoCarpeta.Directo,
        PatronCarpeta = null,
        Renombrar = false,
        Patrones =
        [
            new PatronReconocimiento(1,
            [
                MarcaDeLinea(CampoMarca.Emisor, 0),
                MarcaDeLinea(CampoMarca.Tipo, 1)
            ])
        ]
    };

    private static Marca MarcaDeLinea(CampoMarca campo, int indiceLinea)
    {
        var banda = CreadorPdfDePrueba.ObtenerBandaDeLinea(indiceLinea);
        return new Marca(campo, 0, banda.X, banda.Y, banda.Ancho, banda.Alto);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_ConTextoQueCoincideExacto_DevuelveLaConfiguracion()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Banco de Prueba SA", "Resumen de cuenta");
        var configuracion = CrearConfiguracion("Banco de Prueba SA", "Resumen de cuenta");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, [configuracion]);

        Assert.NotNull(resultado);
        Assert.Equal("Banco de Prueba SA", resultado!.Emisor);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_ConEmisorDistinto_DevuelveNull()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Banco de Prueba SA", "Resumen de cuenta");
        var configuracion = CrearConfiguracion("Otro Banco", "Resumen de cuenta");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, [configuracion]);

        Assert.Null(resultado);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_ConTipoDistinto_DevuelveNull()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Banco de Prueba SA", "Resumen de cuenta");
        var configuracion = CrearConfiguracion("Banco de Prueba SA", "Factura");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, [configuracion]);

        Assert.Null(resultado);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_EsInsensibleAMayusculasYEspacios()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Banco de Prueba SA", "Resumen de cuenta");
        var configuracion = CrearConfiguracion("  banco de prueba sa  ", "RESUMEN DE CUENTA");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, [configuracion]);

        Assert.NotNull(resultado);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_ConVariasConfiguraciones_DevuelveSoloLaQueCoincide()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Banco de Prueba SA", "Resumen de cuenta");
        var configuraciones = new[]
        {
            CrearConfiguracion("Otro Banco", "Otro Tipo"),
            CrearConfiguracion("Banco de Prueba SA", "Resumen de cuenta")
        };

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, configuraciones);

        Assert.NotNull(resultado);
        Assert.Equal("Banco de Prueba SA", resultado!.Emisor);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_SinNingunaConfiguracion_DevuelveNull()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Banco de Prueba SA", "Resumen de cuenta");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, []);

        Assert.Null(resultado);
    }

    public void Dispose()
    {
        Directory.Delete(_carpetaTemporal, recursive: true);
    }
}
