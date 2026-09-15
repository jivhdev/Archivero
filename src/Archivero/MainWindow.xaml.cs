using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Archivero.Datos;
using Archivero.Servicios;
using Archivero.Vistas;

namespace Archivero;

/// <summary>Una entrada del historial de "Guardados automáticamente" (Caso-1, punto 4).</summary>
public record GuardadoReciente(DateTime Hora, string RutaFinal)
{
    public string Resumen => $"{Hora:HH:mm:ss} — {Path.GetFileName(RutaFinal)} → {RutaFinal}";
}

public partial class MainWindow : Window
{
    private readonly PendienteRepository _pendientes = new();
    private readonly ConfiguracionDocumentoRepository _configuraciones = new();
    private readonly CarpetaObservadaService _servicioCarpeta = new(new ConfiguracionRepository());
    private VigilanciaCarpetaService _vigilancia;
    private string _carpetaObservada;
    private readonly TrayIconService _bandeja = new();
    private readonly List<GuardadoReciente> _guardadosRecientes = [];
    private bool _permitirCierre;

    public MainWindow(string carpetaObservada, VigilanciaCarpetaService vigilancia)
    {
        InitializeComponent();
        _carpetaObservada = carpetaObservada;
        TxtCarpetaObservada.Text = $"Carpeta observada: {carpetaObservada}";

        _vigilancia = vigilancia;
        SuscribirEventosVigilancia();

        _bandeja.MostrarVentanaSolicitado += () => Dispatcher.Invoke(RestaurarVentana);
        _bandeja.SalirSolicitado += () => Dispatcher.Invoke(SalirDeVerdad);

        CargarPendientes();
    }

    private void SuscribirEventosVigilancia()
    {
        _vigilancia.ArchivoPendienteDetectado += _ => Dispatcher.Invoke(CargarPendientes);
        _vigilancia.ArchivoPendienteEliminado += _ => Dispatcher.Invoke(CargarPendientes);
        _vigilancia.ArchivoRequiereAtencion += (_, _) => Dispatcher.Invoke(CargarPendientes);
        _vigilancia.ArchivoGuardadoAutomaticamente += (_, rutaFinal) => Dispatcher.Invoke(() => AgregarAGuardadosRecientes(rutaFinal));
        _vigilancia.CarpetaObservadaNoDisponible += () => Dispatcher.Invoke(AvisarCarpetaNoDisponible);
    }

    private void BtnCambiarCarpetaObservada_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new CambiarCarpetaObservadaWindow(_servicioCarpeta, _carpetaObservada) { Owner = this };
        if (ventana.ShowDialog() != true || ventana.CarpetaNueva is null)
        {
            return;
        }

        _vigilancia.Dispose();
        _carpetaObservada = ventana.CarpetaNueva;
        TxtCarpetaObservada.Text = $"Carpeta observada: {_carpetaObservada}";

        _vigilancia = new VigilanciaCarpetaService(_carpetaObservada);
        SuscribirEventosVigilancia();
        _vigilancia.Iniciar();

        CargarPendientes();
    }

    private void CargarPendientes()
    {
        var pendientes = _pendientes.ObtenerTodos();
        ListaPendientes.ItemsSource = pendientes;
        _bandeja.ActualizarPendientes(pendientes.Count > 0);
    }

    private void AgregarAGuardadosRecientes(string rutaFinal)
    {
        _guardadosRecientes.Insert(0, new GuardadoReciente(DateTime.Now, rutaFinal));
        ListaGuardados.ItemsSource = null;
        ListaGuardados.ItemsSource = _guardadosRecientes;
    }

    private void ListaGuardados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ListaGuardados.SelectedItem is not GuardadoReciente guardado)
        {
            return;
        }

        var ventana = new VerGuardadoWindow(guardado.RutaFinal) { Owner = this };
        ventana.ShowDialog();
    }

    private void AvisarCarpetaNoDisponible()
    {
        System.Windows.MessageBox.Show(
            this,
            "La carpeta observada ya no está disponible (se movió o se borró). Archivero no puede seguir vigilándola hasta que vuelva a estar accesible.",
            "Archivero", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void RestaurarVentana()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void SalirDeVerdad()
    {
        _permitirCierre = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_permitirCierre)
        {
            // Cerrar la ventana no apaga Archivero: se minimiza a la bandeja y sigue
            // observando la carpeta en silencio (REQ-005). "Salir" desde la bandeja es la
            // unica forma de terminar el proceso de verdad.
            e.Cancel = true;
            Hide();
            return;
        }

        _bandeja.Dispose();
        base.OnClosing(e);
        System.Windows.Application.Current.Shutdown();
    }

    private void ListaPendientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ListaPendientes.SelectedItem is not ArchivoPendiente pendiente)
        {
            return;
        }

        if (pendiente.Motivo == MotivoPendiente.Duplicado)
        {
            AbrirResolucionDeDuplicado(pendiente.RutaArchivo);
        }
        else if (pendiente.Motivo == MotivoPendiente.PeriodoNuevo)
        {
            AbrirCreacionDePeriodo(pendiente.RutaArchivo);
        }
        else if (pendiente.Motivo == MotivoPendiente.SinTextoExtraible)
        {
            var identificarSinTexto = new IdentificarSinTextoWindow(pendiente.RutaArchivo) { Owner = this };
            identificarSinTexto.ShowDialog();
        }
        else
        {
            var asistente = new IdentificarDocumentoWindow(pendiente.RutaArchivo) { Owner = this };
            asistente.ShowDialog();
        }

        CargarPendientes();
    }

    private void AbrirResolucionDeDuplicado(string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
        {
            return;
        }

        // Se recalcula todo en el momento (coincidencia + campos extraidos) en vez de guardarlo
        // en la base: es el mismo documento y la misma configuracion, asi que da lo mismo, y
        // evita duplicar el estado en Pendientes.
        var configuraciones = _configuraciones.ObtenerTodasConPatrones();
        var coincidencia = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(rutaArchivo, configuraciones);
        if (coincidencia is null)
        {
            // Ya no coincide con ninguna configuracion (por ejemplo, se borro) -> tratarlo
            // como documento nuevo en vez de romper.
            var asistente = new IdentificarDocumentoWindow(rutaArchivo) { Owner = this };
            asistente.ShowDialog();
            return;
        }

        var (campos, error) = GuardadoAutomaticoService.ExtraerCamposParaClasificar(rutaArchivo, coincidencia);
        if (campos is null)
        {
            System.Windows.MessageBox.Show(this,
                $"No se pudo volver a leer los datos de este documento ({error}). Probá abrirlo desde \"Administrar clasificaciones\" para revisar el patrón.",
                "Archivero", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rutaDestinoConflicto = ClasificadorService.CalcularRutaDestino(rutaArchivo, coincidencia, campos.Fecha, campos.NombreExtraido);

        var resolver = new ResolverDuplicadoWindow(rutaArchivo, rutaDestinoConflicto) { Owner = this };
        resolver.ShowDialog();
    }

    private void AbrirCreacionDePeriodo(string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
        {
            return;
        }

        var configuraciones = _configuraciones.ObtenerTodasConPatrones();
        var coincidencia = CoincidenciaAutomaticaService.BuscarConfiguracionQueCoincide(rutaArchivo, configuraciones);
        if (coincidencia is null)
        {
            var asistente = new IdentificarDocumentoWindow(rutaArchivo) { Owner = this };
            asistente.ShowDialog();
            return;
        }

        var (campos, error) = GuardadoAutomaticoService.ExtraerCamposParaClasificar(rutaArchivo, coincidencia);
        if (campos?.Fecha is null)
        {
            System.Windows.MessageBox.Show(this,
                $"No se pudo volver a leer la fecha de este documento ({error}). Probá abrirlo desde \"Administrar clasificaciones\" para revisar el patrón.",
                "Archivero", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var carpetaPeriodo = Path.GetDirectoryName(
            ClasificadorService.CalcularRutaDestino(rutaArchivo, coincidencia, campos.Fecha, campos.NombreExtraido))!;

        var ventana = new CrearPeriodoWindow(rutaArchivo, coincidencia, campos.Fecha.Value, campos.NombreExtraido, carpetaPeriodo) { Owner = this };
        ventana.ShowDialog();
    }

    private void BtnAdministrarClasificaciones_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new AdministrarClasificacionesWindow { Owner = this };
        ventana.ShowDialog();
    }
}
