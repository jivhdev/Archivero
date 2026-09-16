using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Archivero.Datos;
using Archivero.Servicios;
using Archivero.Servicios.Pdf;

namespace Archivero.Vistas;

/// <summary>
/// Resolución de nombre de archivo duplicado (REQ-002): Revisar lado a lado, Reemplazar,
/// Dejar pendiente, o Guardar en otra ubicación como excepción — en ese orden, como pide SPEC.md.
/// </summary>
public partial class ResolverDuplicadoWindow : Window
{
    private readonly string _rutaArchivoNuevo;
    private readonly string _rutaDestinoConflicto;
    private readonly PendienteRepository _pendientes = new();

    public ResolverDuplicadoWindow(string rutaArchivoNuevo, string rutaDestinoConflicto)
    {
        InitializeComponent();
        _rutaArchivoNuevo = rutaArchivoNuevo;
        _rutaDestinoConflicto = rutaDestinoConflicto;

        TxtRutaConflicto.Text =
            $"Documento nuevo: {Path.GetFileName(rutaArchivoNuevo)}\n" +
            $"Ya existe en: {rutaDestinoConflicto}";
    }

    private void BtnRevisar_Click(object sender, RoutedEventArgs e)
    {
        if (PanelComparacion.Visibility == Visibility.Visible)
        {
            PanelComparacion.Visibility = Visibility.Collapsed;
            BtnRevisar.Content = "Revisar lado a lado";
            return;
        }

        try
        {
            ImagenExistente.Source = RenderizarPrimeraPagina(_rutaDestinoConflicto);
            ImagenNuevo.Source = RenderizarPrimeraPagina(_rutaArchivoNuevo);
            PanelComparacion.Visibility = Visibility.Visible;
            BtnRevisar.Content = "Ocultar comparación";

            // Caso-5: "Eliminar duplicado" solo se puede habilitar despues de usar "Revisar" al
            // menos una vez -- la confirmacion (el checkbox) no aparece antes de eso.
            ChkConfirmarMismoDocumento.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"No se pudo mostrar la comparación: {ex.Message}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ChkConfirmarMismoDocumento_Changed(object sender, RoutedEventArgs e)
    {
        BtnEliminarDuplicado.IsEnabled = ChkConfirmarMismoDocumento.IsChecked == true;
    }

    /// <summary>Caso-5: al revés de "Reemplazar" -- se queda el archivo VIEJO tal cual estaba, se descarta el que acaba de llegar.</summary>
    private void BtnEliminarDuplicado_Click(object sender, RoutedEventArgs e)
    {
        var confirmar = System.Windows.MessageBox.Show(
            this,
            $"¿Eliminar el documento nuevo que acaba de llegar?\n\n{_rutaArchivoNuevo}\n\n" +
            "El que ya estaba guardado no se toca.",
            "Archivero", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirmar != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            File.Delete(_rutaArchivoNuevo);
            _pendientes.Quitar(_rutaArchivoNuevo);

            System.Windows.MessageBox.Show(this, "Documento nuevo eliminado. Se mantuvo el que ya estaba guardado.", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"No se pudo eliminar: {ex.Message}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static BitmapSource RenderizarPrimeraPagina(string rutaPdf)
    {
        var pagina = LectorPdf.RenderizarPagina(rutaPdf, 0);
        var bitmap = BitmapSource.Create(
            pagina.Ancho, pagina.Alto, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null,
            pagina.PixelesBgra, pagina.Ancho * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private void BtnReemplazar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ClasificadorService.ReemplazarYClasificar(_rutaArchivoNuevo, _rutaDestinoConflicto);
            _pendientes.Quitar(_rutaArchivoNuevo);

            System.Windows.MessageBox.Show(this, $"Reemplazado:\n{_rutaDestinoConflicto}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"No se pudo reemplazar: {ex.Message}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDejarPendiente_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnExcepcion_Click(object sender, RoutedEventArgs e)
    {
        using var dialogo = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Elegir dónde guardar este documento como excepción"
        };

        if (dialogo.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var nombreArchivo = Path.GetFileName(_rutaDestinoConflicto);
        var rutaExcepcion = Path.Combine(dialogo.SelectedPath, nombreArchivo);

        try
        {
            ClasificadorService.GuardarComoExcepcion(_rutaArchivoNuevo, rutaExcepcion);
            _pendientes.Quitar(_rutaArchivoNuevo);

            System.Windows.MessageBox.Show(this, $"Guardado como excepción en:\n{rutaExcepcion}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (ArchivoDuplicadoException)
        {
            System.Windows.MessageBox.Show(this,
                $"También existe un archivo con ese nombre ahí:\n{rutaExcepcion}\nElegí otra carpeta.",
                "Archivero", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"No se pudo guardar: {ex.Message}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
