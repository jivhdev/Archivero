using Archivero.Datos;
using Archivero.Servicios;

namespace Archivero.Tests;

public class VigilanciaCarpetaServiceTests : IDisposable
{
    private readonly string _rutaDbTemporal;
    private readonly string _rutaLogOriginal;

    public VigilanciaCarpetaServiceTests()
    {
        _rutaDbTemporal = Path.Combine(Path.GetTempPath(), $"archivero-tests-{Guid.NewGuid():N}.db");
        BaseDeDatos.RutaArchivo = _rutaDbTemporal;
        BaseDeDatos.AsegurarEsquema();

        // Caso-9, mejora 2: redirigir el log de auditoria para no escribir en el real del
        // usuario al correr los tests.
        _rutaLogOriginal = AuditoriaService.RutaLog;
        AuditoriaService.RutaLog = Path.Combine(Path.GetTempPath(), $"archivero-tests-{Guid.NewGuid():N}", "auditoria.log");
    }

    [Fact]
    public void Iniciar_ConLaCarpetaObservadaInexistente_AvisaEnVezDeQuedarseCallado()
    {
        // Bug real reportado por Javier: la carpeta observada se borró del disco por fuera de
        // Archivero, y al reabrirlo no avisaba nada -- se quedaba observando en silencio total,
        // sin importar qué archivos se dejaran ahí. REQ-005 pide avisar, no fallar en silencio.
        var carpetaQueNoExiste = Path.Combine(Path.GetTempPath(), "archivero-tests-inexistente-" + Guid.NewGuid().ToString("N"));
        using var vigilancia = new VigilanciaCarpetaService(carpetaQueNoExiste);

        var avisoDisparado = false;
        vigilancia.CarpetaObservadaNoDisponible += () => avisoDisparado = true;

        vigilancia.Iniciar();

        Assert.True(avisoDisparado);
    }

    [Fact]
    public void Iniciar_ConLaCarpetaObservadaExistente_NoAvisa()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "archivero-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);

        try
        {
            using var vigilancia = new VigilanciaCarpetaService(carpeta);

            var avisoDisparado = false;
            vigilancia.CarpetaObservadaNoDisponible += () => avisoDisparado = true;

            vigilancia.Iniciar();

            Assert.False(avisoDisparado);
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    public void Dispose()
    {
        AuditoriaService.RutaLog = _rutaLogOriginal;
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_rutaDbTemporal);
    }
}
