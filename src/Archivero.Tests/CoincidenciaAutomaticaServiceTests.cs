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

    [Fact]
    public void BuscarConfiguracionQueCoincide_ConTextoDeReferenciaDistintoDelNombreVisible_ComparaContraElTextoMarcado()
    {
        // Reproduce el caso real: el nombre del emisor es un logo (no se puede extraer como
        // texto), asi que se marco el RUT en su lugar, pero la entidad se llama distinto.
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "RUT-12.345.678-9", "Factura Electronica");
        var configuracion = ConfiguracionConTextoReferencia(
            emisorVisible: "Veliz Beltran Limitada", textoReferenciaEmisor: "RUT-12.345.678-9",
            tipoVisible: "Factura Electronica", textoReferenciaTipo: "Factura Electronica");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, [configuracion]);

        Assert.NotNull(resultado);
        Assert.Equal("Veliz Beltran Limitada", resultado!.Emisor);
    }

    [Fact]
    public void BuscarConfiguracionQueCoincide_ConTextoDeReferencia_NoCoincideSoloPorqueElNombreVisibleCoincidiria()
    {
        // Si el patron tiene TextoReferencia guardado, se compara contra ESO, no contra el
        // nombre visible de la entidad, aunque el nombre visible aparezca igual en el PDF.
        var ruta = CreadorPdfDePrueba.CrearConLineas(_carpetaTemporal, "Veliz Beltran Limitada", "Factura Electronica");
        var configuracion = ConfiguracionConTextoReferencia(
            emisorVisible: "Veliz Beltran Limitada", textoReferenciaEmisor: "RUT-99.999.999-9",
            tipoVisible: "Factura Electronica", textoReferenciaTipo: "Factura Electronica");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(ruta, [configuracion]);

        Assert.Null(resultado);
    }

    private static ConfiguracionDocumento ConfiguracionConTextoReferencia(
        string emisorVisible, string textoReferenciaEmisor, string tipoVisible, string textoReferenciaTipo)
    {
        var bandaEmisor = CreadorPdfDePrueba.ObtenerBandaDeLinea(0);
        var bandaTipo = CreadorPdfDePrueba.ObtenerBandaDeLinea(1);

        return new ConfiguracionDocumento
        {
            Id = 1,
            Emisor = emisorVisible,
            Tipo = tipoVisible,
            CarpetaDestino = @"C:\Destino",
            FormatoCarpeta = FormatoCarpeta.Directo,
            PatronCarpeta = null,
            Renombrar = false,
            Patrones =
            [
                new PatronReconocimiento(1,
                [
                    new Marca(CampoMarca.Emisor, 0, bandaEmisor.X, bandaEmisor.Y, bandaEmisor.Ancho, bandaEmisor.Alto, textoReferenciaEmisor),
                    new Marca(CampoMarca.Tipo, 0, bandaTipo.X, bandaTipo.Y, bandaTipo.Ancho, bandaTipo.Alto, textoReferenciaTipo)
                ])
            ]
        };
    }

    [Fact]
    public void BuscarConfiguracionSinTextoQueCoincide_ConUnaSolaConfiguracionSinTexto_LaDevuelve()
    {
        var conTexto = CrearConfiguracion("Banco de Prueba SA", "Resumen de cuenta");
        var sinTexto = ConfiguracionConPatronSinMarcas("Proveedor Escaneado", "Recibo escaneado");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionSinTextoQueCoincide([conTexto, sinTexto]);

        Assert.NotNull(resultado);
        Assert.Equal("Proveedor Escaneado", resultado!.Emisor);
        Assert.Empty(resultado.Patrones.Single().Marcas);
    }

    [Fact]
    public void BuscarConfiguracionSinTextoQueCoincide_SinNingunaConfiguracionSinTexto_DevuelveNull()
    {
        var conTexto = CrearConfiguracion("Banco de Prueba SA", "Resumen de cuenta");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionSinTextoQueCoincide([conTexto]);

        Assert.Null(resultado);
    }

    [Fact]
    public void BuscarConfiguracionSinTextoQueCoincide_ConMasDeUnaConfiguracionSinTexto_EsAmbiguoYDevuelveNull()
    {
        // Sin texto que comparar, no hay forma de distinguir a cual corresponde: nunca se
        // adivina entre varias, se deja pendiente para que el usuario decida.
        var primera = ConfiguracionConPatronSinMarcas("Proveedor Uno", "Recibo escaneado");
        var segunda = ConfiguracionConPatronSinMarcas("Proveedor Dos", "Boleta escaneada");

        var resultado = CoincidenciaAutomaticaService.BuscarConfiguracionSinTextoQueCoincide([primera, segunda]);

        Assert.Null(resultado);
    }

    private static ConfiguracionDocumento ConfiguracionConPatronSinMarcas(string emisor, string tipo) => new()
    {
        Id = 2,
        Emisor = emisor,
        Tipo = tipo,
        CarpetaDestino = @"C:\Destino",
        FormatoCarpeta = FormatoCarpeta.Directo,
        PatronCarpeta = null,
        Renombrar = false,
        Patrones = [new PatronReconocimiento(1, [])]
    };

    public void Dispose()
    {
        Directory.Delete(_carpetaTemporal, recursive: true);
    }
}
