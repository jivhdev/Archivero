using Archivero.Datos;

namespace Archivero.Servicios;

public class VigilanciaCarpetaService : IDisposable
{
    private readonly string _carpetaObservada;
    private readonly PendienteRepository _pendientes = new();
    private FileSystemWatcher? _watcher;

    public event Action<string>? ArchivoPendienteDetectado;

    public VigilanciaCarpetaService(string carpetaObservada)
    {
        _carpetaObservada = carpetaObservada;
    }

    public void Iniciar()
    {
        RevisarArchivosExistentes();

        if (!Directory.Exists(_carpetaObservada))
        {
            return;
        }

        _watcher = new FileSystemWatcher(_carpetaObservada, "*.pdf")
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true
        };
        _watcher.Created += (_, e) => ProcesarArchivo(e.FullPath);
    }

    private void RevisarArchivosExistentes()
    {
        if (!Directory.Exists(_carpetaObservada))
        {
            return;
        }

        foreach (var archivo in Directory.GetFiles(_carpetaObservada, "*.pdf"))
        {
            ProcesarArchivo(archivo);
        }
    }

    private void ProcesarArchivo(string rutaArchivo)
    {
        // REQ-002 (coincidencia automatica contra configuraciones existentes) todavia no esta
        // implementado: por ahora, todo PDF nuevo en la carpeta observada pasa a pendientes.
        if (_pendientes.Agregar(rutaArchivo))
        {
            ArchivoPendienteDetectado?.Invoke(rutaArchivo);
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
