using System.IO;
using Archivero.Datos;
using Archivero.Servicios.Pdf;

namespace Archivero.Servicios;

public class VigilanciaCarpetaService : IDisposable
{
    private readonly string _carpetaObservada;
    private readonly PendienteRepository _pendientes = new();
    private readonly BorradorRepository _borradores = new();
    private readonly ConfiguracionDocumentoRepository _configuraciones = new();
    private FileSystemWatcher? _watcher;

    /// <summary>Un PDF nuevo no coincide con ninguna configuración: pasa al flujo de identificación (REQ-003).</summary>
    public event Action<string>? ArchivoPendienteDetectado;

    /// <summary>Un archivo que estaba en pendientes se sacó de la carpeta observada (a mano, por fuera de Archivero).</summary>
    public event Action<string>? ArchivoPendienteEliminado;

    /// <summary>Un PDF se reconoció y se guardó solo en su ubicación definitiva.</summary>
    public event Action<string, string>? ArchivoGuardadoAutomaticamente;

    /// <summary>Un PDF coincidió con una configuración pero algo impidió guardarlo solo (queda pendiente, hay que avisar).</summary>
    public event Action<string, string>? ArchivoRequiereAtencion;

    /// <summary>La carpeta observada dejó de existir (se borró o se movió) mientras Archivero corría.</summary>
    public event Action? CarpetaObservadaNoDisponible;

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
        // FileSystemWatcher entrega los eventos Created de a uno, en orden, en un unico hilo de
        // fondo: como este handler es sincronico (sin async/Task.Run), los archivos se procesan
        // de a uno y nunca en paralelo, tal como pide SPEC.md.
        _watcher.Created += (_, e) => ProcesarArchivo(e.FullPath);

        // Si el usuario saca un archivo de la carpeta observada por fuera de Archivero
        // (lo borra, lo mueve a mano) mientras estaba en pendientes, hay que reflejarlo:
        // si no, la lista de pendientes queda mostrando un archivo que ya no existe.
        _watcher.Deleted += (_, e) => ManejarArchivoEliminado(e.FullPath);
        _watcher.Renamed += (_, e) =>
        {
            ManejarArchivoEliminado(e.OldFullPath);
            ProcesarArchivo(e.FullPath);
        };

        // Si la carpeta observada se borra o se mueve mientras Archivero esta corriendo, el
        // FileSystemWatcher dispara Error en vez de quedarse callado.
        _watcher.Error += (_, _) => CarpetaObservadaNoDisponible?.Invoke();
    }

    private void ManejarArchivoEliminado(string rutaArchivo)
    {
        _borradores.Eliminar(rutaArchivo);
        if (_pendientes.Quitar(rutaArchivo))
        {
            ArchivoPendienteEliminado?.Invoke(rutaArchivo);
        }
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
        try
        {
            if (!File.Exists(rutaArchivo))
            {
                return;
            }

            if (!LectorPdf.TieneTextoExtraible(rutaArchivo))
            {
                // No es un PDF con texto plano extraible (ej. una imagen escaneada): no se
                // procesa, el usuario lo guarda a mano (RNF-3: nunca se toca ni se mueve).
                return;
            }

            var configuraciones = _configuraciones.ObtenerTodasConPatrones();
            var coincidencia = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(rutaArchivo, configuraciones);

            if (coincidencia is null)
            {
                AgregarAPendientes(rutaArchivo);
                return;
            }

            var resultado = GuardadoAutomaticoService.Procesar(rutaArchivo, coincidencia);
            switch (resultado.Resultado)
            {
                case ResultadoGuardadoAutomatico.Guardado:
                    _pendientes.Quitar(rutaArchivo);
                    ArchivoGuardadoAutomaticamente?.Invoke(rutaArchivo, resultado.RutaFinal!);
                    break;

                case ResultadoGuardadoAutomatico.ValorInvalido:
                    AgregarAPendientes(rutaArchivo);
                    break;

                case ResultadoGuardadoAutomatico.Duplicado:
                case ResultadoGuardadoAutomatico.CarpetaNoDisponible:
                    if (AgregarAPendientes(rutaArchivo))
                    {
                        ArchivoRequiereAtencion?.Invoke(rutaArchivo, resultado.Detalle ?? resultado.Resultado.ToString());
                    }
                    break;
            }
        }
        catch
        {
            // RNF-2: si el archivo esta corrupto o no se puede leer, Archivero deja de intentar
            // con ese archivo puntual (sin tocarlo ni moverlo) y sigue observando con normalidad.
        }
    }

    private bool AgregarAPendientes(string rutaArchivo)
    {
        var esNuevo = _pendientes.Agregar(rutaArchivo);
        if (esNuevo)
        {
            ArchivoPendienteDetectado?.Invoke(rutaArchivo);
        }

        return esNuevo;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
