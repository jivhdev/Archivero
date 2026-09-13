using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Archivero.Datos;
using Archivero.Servicios;
using Archivero.Vistas;

namespace Archivero;

public partial class MainWindow : Window
{
    private readonly PendienteRepository _pendientes = new();
    private readonly VigilanciaCarpetaService _vigilancia;
    private readonly TrayIconService _bandeja = new();
    private bool _permitirCierre;

    public MainWindow(string carpetaObservada, VigilanciaCarpetaService vigilancia)
    {
        InitializeComponent();
        TxtCarpetaObservada.Text = $"Carpeta observada: {carpetaObservada}";

        _vigilancia = vigilancia;
        _vigilancia.ArchivoPendienteDetectado += _ => Dispatcher.Invoke(CargarPendientes);
        _vigilancia.ArchivoRequiereAtencion += (_, _) => Dispatcher.Invoke(CargarPendientes);
        _vigilancia.ArchivoGuardadoAutomaticamente += (_, rutaFinal) => Dispatcher.Invoke(() => AgregarAGuardadosRecientes(rutaFinal));
        _vigilancia.CarpetaObservadaNoDisponible += () => Dispatcher.Invoke(AvisarCarpetaNoDisponible);

        _bandeja.MostrarVentanaSolicitado += () => Dispatcher.Invoke(RestaurarVentana);
        _bandeja.SalirSolicitado += () => Dispatcher.Invoke(SalirDeVerdad);

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
        ListaGuardados.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} — {Path.GetFileName(rutaFinal)} → {rutaFinal}");
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

        var asistente = new IdentificarDocumentoWindow(pendiente.RutaArchivo) { Owner = this };
        asistente.ShowDialog();

        CargarPendientes();
    }

    private void BtnAdministrarClasificaciones_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new AdministrarClasificacionesWindow { Owner = this };
        ventana.ShowDialog();
    }
}
