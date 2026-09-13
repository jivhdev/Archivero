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

    public MainWindow(string carpetaObservada, VigilanciaCarpetaService vigilancia)
    {
        InitializeComponent();
        TxtCarpetaObservada.Text = $"Carpeta observada: {carpetaObservada}";

        _vigilancia = vigilancia;
        _vigilancia.ArchivoPendienteDetectado += _ => Dispatcher.Invoke(CargarPendientes);

        CargarPendientes();
    }

    private void CargarPendientes()
    {
        ListaPendientes.ItemsSource = _pendientes.ObtenerTodos();
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
}
