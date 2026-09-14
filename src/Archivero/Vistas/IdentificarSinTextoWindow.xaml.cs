using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Archivero.Datos;
using Archivero.Servicios;
using Archivero.Servicios.Pdf;

namespace Archivero.Vistas;

/// <summary>
/// Identificación de un PDF sin texto extraíble (Caso-1, punto 1): no se puede marcar nada por
/// coordenadas, así que Emisor y Tipo se escriben a mano, y la carpeta se elige como en
/// cualquier configuración. Sin fecha ni nombre extraíbles, la configuración queda siempre en
/// "Directo" y sin renombrar — solo ubicación automática para los próximos documentos iguales.
/// </summary>
public partial class IdentificarSinTextoWindow : Window
{
    private readonly string _rutaArchivo;
    private readonly EntidadRepository _entidades = new();
    private readonly ConfiguracionDocumentoRepository _configuraciones = new();
    private readonly PendienteRepository _pendientes = new();
    private string _carpetaDestino = string.Empty;

    public IdentificarSinTextoWindow(string rutaArchivo)
    {
        InitializeComponent();
        _rutaArchivo = rutaArchivo;

        CmbEmisor.ItemsSource = _entidades.Buscar(CategoriaEntidad.Emisor, string.Empty);
        CmbTipo.ItemsSource = _entidades.Buscar(CategoriaEntidad.Tipo, string.Empty);

        try
        {
            var pagina = LectorPdf.RenderizarPagina(rutaArchivo, 0);
            var bitmap = BitmapSource.Create(
                pagina.Ancho, pagina.Alto, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null,
                pagina.PixelesBgra, pagina.Ancho * 4);
            bitmap.Freeze();
            ImagenDocumento.Source = bitmap;
        }
        catch
        {
            // Sin vista previa si no se puede renderizar (ej. imagen en un formato raro): no
            // bloquea el resto del flujo, igual se puede identificar a mano.
        }
    }

    private void CmbEmisor_TextChanged(object sender, TextChangedEventArgs e)
    {
        CmbEmisor.ItemsSource = _entidades.Buscar(CategoriaEntidad.Emisor, CmbEmisor.Text);
    }

    private void CmbTipo_TextChanged(object sender, TextChangedEventArgs e)
    {
        CmbTipo.ItemsSource = _entidades.Buscar(CategoriaEntidad.Tipo, CmbTipo.Text);
    }

    private void BtnElegirCarpeta_Click(object sender, RoutedEventArgs e)
    {
        using var dialogo = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Elegir la carpeta de destino para este Emisor y Tipo"
        };

        if (dialogo.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _carpetaDestino = dialogo.SelectedPath;
            TxtCarpetaDestino.Text = _carpetaDestino;
        }
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var emisor = CmbEmisor.Text.Trim();
        var tipo = CmbTipo.Text.Trim();

        if (string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(tipo))
        {
            MostrarError("Completar el Emisor y el Tipo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_carpetaDestino))
        {
            MostrarError("Elegir una carpeta de destino.");
            return;
        }

        try
        {
            var existente = _configuraciones.BuscarPorEmisorYTipo(emisor, tipo);
            ConfiguracionDocumento configuracionParaClasificar;

            if (existente is not null)
            {
                var vincular = System.Windows.MessageBox.Show(
                    this,
                    $"Ya existe una configuración guardada para \"{emisor}\" / \"{tipo}\".\n\n" +
                    "¿Vincular este documento a esa configuración (misma carpeta de destino ya definida)? " +
                    "Si esa configuración organiza por fecha, los próximos documentos sin texto de este " +
                    "tipo van a quedar pendientes igualmente (no hay fecha que extraer de una imagen).",
                    "Archivero", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (vincular != MessageBoxResult.Yes)
                {
                    MostrarError("Corregir el Emisor o el Tipo si no correspondía, o cancelar.");
                    return;
                }

                _configuraciones.AgregarPatronAConfiguracionExistente(existente.Id, []);
                configuracionParaClasificar = existente;
            }
            else
            {
                var nuevaId = _configuraciones.GuardarNueva(emisor, tipo, _carpetaDestino, FormatoCarpeta.Directo, null, false, []);
                configuracionParaClasificar = new ConfiguracionDocumento
                {
                    Id = nuevaId, Emisor = emisor, Tipo = tipo, CarpetaDestino = _carpetaDestino,
                    FormatoCarpeta = FormatoCarpeta.Directo, PatronCarpeta = null, Renombrar = false, Patrones = []
                };
            }

            string rutaFinal;
            try
            {
                rutaFinal = ClasificadorService.Clasificar(_rutaArchivo, configuracionParaClasificar, null, null);
            }
            catch (ArchivoDuplicadoException ex)
            {
                var resolver = new ResolverDuplicadoWindow(_rutaArchivo, ex.RutaDestino) { Owner = this };
                if (resolver.ShowDialog() != true)
                {
                    MostrarError("Documento dejado pendiente por nombre duplicado. Podés intentar de nuevo o cancelar.");
                    return;
                }

                rutaFinal = ex.RutaDestino;
            }

            _pendientes.Quitar(_rutaArchivo);

            System.Windows.MessageBox.Show(this, $"Documento guardado en:\n{rutaFinal}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo guardar: {ex.Message}");
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void MostrarError(string mensaje)
    {
        TxtError.Text = mensaje;
        TxtError.Visibility = Visibility.Visible;
    }
}
