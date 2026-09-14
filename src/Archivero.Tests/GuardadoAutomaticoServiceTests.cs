using Archivero.Datos;
using Archivero.Servicios;

namespace Archivero.Tests;

public class GuardadoAutomaticoServiceTests : IDisposable
{
    private readonly string _raiz;
    private readonly string _carpetaOrigen;
    private readonly string _carpetaDestino;

    public GuardadoAutomaticoServiceTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "archivero-tests-" + Guid.NewGuid().ToString("N"));
        _carpetaOrigen = Path.Combine(_raiz, "origen");
        _carpetaDestino = Path.Combine(_raiz, "destino");
        Directory.CreateDirectory(_carpetaOrigen);
        Directory.CreateDirectory(_carpetaDestino);
    }

    private static Marca MarcaDeLinea(CampoMarca campo, int indiceLinea)
    {
        var banda = CreadorPdfDePrueba.ObtenerBandaDeLinea(indiceLinea);
        return new Marca(campo, 0, banda.X, banda.Y, banda.Ancho, banda.Alto);
    }

    [Fact]
    public void Procesar_ConFormatoDirecto_ClasificaElArchivoSinSubcarpetas()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaOrigen, "Banco de Prueba SA", "Resumen de cuenta");
        var configuracion = new ConfiguracionDocumento
        {
            Id = 1,
            Emisor = "Banco de Prueba SA",
            Tipo = "Resumen de cuenta",
            CarpetaDestino = _carpetaDestino,
            FormatoCarpeta = FormatoCarpeta.Directo,
            PatronCarpeta = null,
            Renombrar = false,
            Patrones = [new PatronReconocimiento(1, [MarcaDeLinea(CampoMarca.Emisor, 0), MarcaDeLinea(CampoMarca.Tipo, 1)])]
        };

        var resultado = GuardadoAutomaticoService.Procesar(ruta, configuracion);

        Assert.Equal(ResultadoGuardadoAutomatico.Guardado, resultado.Resultado);
        Assert.True(File.Exists(resultado.RutaFinal));
        Assert.False(File.Exists(ruta));
    }

    [Fact]
    public void Procesar_ConFormatoAnioYCarpetaDelPeriodoYaExiste_GuardaSolo()
    {
        // Caso comun (silencioso, sin intervencion): la carpeta del periodo actual ya existe,
        // asi que Archivero guarda directo, como pide REQ-002.
        Directory.CreateDirectory(Path.Combine(_carpetaDestino, "2026"));
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaOrigen, "Banco de Prueba SA", "Resumen de cuenta", "12/09/2026");
        var configuracion = new ConfiguracionDocumento
        {
            Id = 1,
            Emisor = "Banco de Prueba SA",
            Tipo = "Resumen de cuenta",
            CarpetaDestino = _carpetaDestino,
            FormatoCarpeta = FormatoCarpeta.Anio,
            PatronCarpeta = "yyyy",
            Renombrar = false,
            Patrones =
            [
                new PatronReconocimiento(1,
                [
                    MarcaDeLinea(CampoMarca.Emisor, 0),
                    MarcaDeLinea(CampoMarca.Tipo, 1),
                    MarcaDeLinea(CampoMarca.Fecha, 2)
                ])
            ]
        };

        var resultado = GuardadoAutomaticoService.Procesar(ruta, configuracion);

        Assert.Equal(ResultadoGuardadoAutomatico.Guardado, resultado.Resultado);
        Assert.Equal(Path.Combine(_carpetaDestino, "2026"), Path.GetDirectoryName(resultado.RutaFinal));
    }

    [Fact]
    public void Procesar_ConFormatoAnioYCarpetaDelPeriodoTodaviaNoExiste_DevuelvePeriodoNuevoYNoLaCrea()
    {
        // Caso-1, punto 2 (ultimo parrafo): la carpeta del periodo actual todavia no existe --
        // Archivero nunca la crea sola, se lo deja a una decision activa del usuario.
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaOrigen, "Banco de Prueba SA", "Resumen de cuenta", "12/09/2026");
        var configuracion = new ConfiguracionDocumento
        {
            Id = 1,
            Emisor = "Banco de Prueba SA",
            Tipo = "Resumen de cuenta",
            CarpetaDestino = _carpetaDestino,
            FormatoCarpeta = FormatoCarpeta.Anio,
            PatronCarpeta = "yyyy",
            Renombrar = false,
            Patrones =
            [
                new PatronReconocimiento(1,
                [
                    MarcaDeLinea(CampoMarca.Emisor, 0),
                    MarcaDeLinea(CampoMarca.Tipo, 1),
                    MarcaDeLinea(CampoMarca.Fecha, 2)
                ])
            ]
        };

        var resultado = GuardadoAutomaticoService.Procesar(ruta, configuracion);

        Assert.Equal(ResultadoGuardadoAutomatico.PeriodoNuevo, resultado.Resultado);
        Assert.False(Directory.Exists(Path.Combine(_carpetaDestino, "2026")));
        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void Procesar_ConFormatoAnioYFechaInvalida_DevuelveValorInvalidoYNoTocaElOriginal()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaOrigen, "Banco de Prueba SA", "Resumen de cuenta", "esto no es una fecha");
        var configuracion = new ConfiguracionDocumento
        {
            Id = 1,
            Emisor = "Banco de Prueba SA",
            Tipo = "Resumen de cuenta",
            CarpetaDestino = _carpetaDestino,
            FormatoCarpeta = FormatoCarpeta.Anio,
            PatronCarpeta = "yyyy",
            Renombrar = false,
            Patrones =
            [
                new PatronReconocimiento(1,
                [
                    MarcaDeLinea(CampoMarca.Emisor, 0),
                    MarcaDeLinea(CampoMarca.Tipo, 1),
                    MarcaDeLinea(CampoMarca.Fecha, 2)
                ])
            ]
        };

        var resultado = GuardadoAutomaticoService.Procesar(ruta, configuracion);

        Assert.Equal(ResultadoGuardadoAutomatico.ValorInvalido, resultado.Resultado);
        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void Procesar_ConRenombrar_UsaElCampoExtraidoComoNombreDeArchivo()
    {
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaOrigen, "Banco de Prueba SA", "Resumen de cuenta", "FACTURA-001");
        var configuracion = new ConfiguracionDocumento
        {
            Id = 1,
            Emisor = "Banco de Prueba SA",
            Tipo = "Resumen de cuenta",
            CarpetaDestino = _carpetaDestino,
            FormatoCarpeta = FormatoCarpeta.Directo,
            PatronCarpeta = null,
            Renombrar = true,
            Patrones =
            [
                new PatronReconocimiento(1,
                [
                    MarcaDeLinea(CampoMarca.Emisor, 0),
                    MarcaDeLinea(CampoMarca.Tipo, 1),
                    MarcaDeLinea(CampoMarca.NombreArchivo, 2)
                ])
            ]
        };

        var resultado = GuardadoAutomaticoService.Procesar(ruta, configuracion);

        Assert.Equal(ResultadoGuardadoAutomatico.Guardado, resultado.Resultado);
        Assert.Equal("FACTURA-001.pdf", Path.GetFileName(resultado.RutaFinal));
    }

    public void Dispose()
    {
        Directory.Delete(_raiz, recursive: true);
    }
}
