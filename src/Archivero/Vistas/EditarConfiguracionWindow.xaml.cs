using System.Windows;
using System.Windows.Forms;
using Archivero.Datos;

namespace Archivero.Vistas;

public partial class EditarConfiguracionWindow : Window
{
    private readonly ConfiguracionDocumento _configuracion;
    private readonly ConfiguracionDocumentoRepository _configuraciones = new();

    public EditarConfiguracionWindow(ConfiguracionDocumento configuracion)
    {
        InitializeComponent();
        _configuracion = configuracion;

        TxtEncabezado.Text = $"{configuracion.Emisor} — {configuracion.Tipo}";
        TxtCarpetaDestino.Text = configuracion.CarpetaDestino;

        var tieneMarcaFecha = configuracion.Patrones.Any(p => p.Marcas.Any(m => m.Campo == CampoMarca.Fecha));
        var tieneMarcaNombre = configuracion.Patrones.Any(p => p.Marcas.Any(m => m.Campo == CampoMarca.NombreArchivo));

        RbAnio.IsEnabled = tieneMarcaFecha;
        RbAnioMes.IsEnabled = tieneMarcaFecha;
        RbExtraerNombre.IsEnabled = tieneMarcaNombre;

        RbDirecto.IsChecked = configuracion.FormatoCarpeta == FormatoCarpeta.Directo;
        RbAnio.IsChecked = configuracion.FormatoCarpeta == FormatoCarpeta.Anio;
        RbAnioMes.IsChecked = configuracion.FormatoCarpeta == FormatoCarpeta.AnioMes;
        if (configuracion.PatronCarpeta is not null)
        {
            CmbPatronCarpeta.Text = configuracion.PatronCarpeta;
        }

        RbMantenerNombre.IsChecked = !configuracion.Renombrar;
        RbExtraerNombre.IsChecked = configuracion.Renombrar;

        if (!tieneMarcaFecha || !tieneMarcaNombre)
        {
            TxtAviso.Text = "Algunas opciones están deshabilitadas porque ningún patrón de este Emisor+Tipo tiene esa coordenada marcada todavía (hace falta vincular un documento nuevo para agregarla).";
        }
    }

    private void BtnCambiarCarpeta_Click(object sender, RoutedEventArgs e)
    {
        using var dialogo = new FolderBrowserDialog
        {
            SelectedPath = TxtCarpetaDestino.Text,
            Description = "Elegir la carpeta de destino"
        };

        if (dialogo.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtCarpetaDestino.Text = dialogo.SelectedPath;
        }
    }

    private void OpcionFormato_Changed(object sender, RoutedEventArgs e)
    {
        var esDirecto = RbDirecto.IsChecked == true;
        var esAnioMes = RbAnioMes.IsChecked == true;

        PanelPatronCarpeta.Visibility = esDirecto ? Visibility.Collapsed : Visibility.Visible;

        if (esDirecto)
        {
            return;
        }

        var conocidos = _configuraciones.ObtenerPatronesDeCarpetaConocidos();
        var presets = esAnioMes ? new[] { "yyyy\\MM", "yyyy\\MMMM" } : new[] { "yyyy", "yy" };
        var opciones = presets.Concat(conocidos.Where(p => esAnioMes == p.Contains('\\'))).Distinct().ToList();

        CmbPatronCarpeta.ItemsSource = opciones;
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCarpetaDestino.Text))
        {
            MostrarAviso("Elegir una carpeta de destino.");
            return;
        }

        FormatoCarpeta? formato = RbDirecto.IsChecked == true ? FormatoCarpeta.Directo
            : RbAnio.IsChecked == true ? FormatoCarpeta.Anio
            : RbAnioMes.IsChecked == true ? FormatoCarpeta.AnioMes
            : null;

        if (formato is null)
        {
            MostrarAviso("Elegir un formato de carpeta.");
            return;
        }

        string? patron = null;
        if (formato != FormatoCarpeta.Directo)
        {
            patron = CmbPatronCarpeta.Text.Trim();
            if (string.IsNullOrWhiteSpace(patron))
            {
                MostrarAviso("Definir el patrón de la subcarpeta.");
                return;
            }
        }

        if (RbMantenerNombre.IsChecked != true && RbExtraerNombre.IsChecked != true)
        {
            MostrarAviso("Elegir cómo se va a llamar el archivo.");
            return;
        }

        var renombrar = RbExtraerNombre.IsChecked == true;

        _configuraciones.ActualizarDestino(_configuracion.Id, TxtCarpetaDestino.Text, formato.Value, patron, renombrar);
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void MostrarAviso(string mensaje)
    {
        TxtAviso.Text = mensaje;
    }
}
