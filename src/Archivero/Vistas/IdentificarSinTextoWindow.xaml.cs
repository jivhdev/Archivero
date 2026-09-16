using System.IO;
using System.Windows;
using System.Windows.Controls;
using Archivero.Datos;
using Archivero.Servicios;

namespace Archivero.Vistas;

/// <summary>
/// Identificación de un PDF sin texto extraíble (Caso-4, reemplaza por completo el flujo del
/// Caso-1 punto 1): no hay Emisor/Tipo ni reconocimiento automático futuro -- solo elegir dónde
/// guardar, reutilizando el asistente de tipo/patrón de Caso-3 (<see cref="OrganizacionCarpetaControl"/>),
/// o volviendo a una ubicación ya usada antes.
///
/// Nota de implementación: Caso-4 pide "marcar la fecha sobre el PDF de la misma forma que ya se
/// hace para otros campos", pero un documento sin texto extraíble no tiene NADA que extraer de
/// una coordenada (por definición: <see cref="Archivero.Servicios.Pdf.LectorPdf.TieneTextoExtraible"/>
/// ya descartó el documento entero). Marcar un rectángulo ahí no puede producir un valor real, así
/// que la fecha se escribe a mano en un campo de texto en vez de marcarse por coordenadas.
/// </summary>
public partial class IdentificarSinTextoWindow : Window
{
    private class FilaUbicacion(UbicacionSinTexto ubicacion)
    {
        public UbicacionSinTexto Ubicacion { get; } = ubicacion;
        public string Texto { get; } = ubicacion.Formato == FormatoCarpeta.Directo
            ? ubicacion.CarpetaMadre
            : $"{ubicacion.CarpetaMadre} — {OrganizacionCarpetaService.NombreDe(ubicacion.Formato)}";
    }

    private readonly string _rutaArchivo;
    private readonly PendienteRepository _pendientes = new();
    private readonly UbicacionSinTextoRepository _ubicaciones = new();

    private string _carpetaMadre = string.Empty;
    private string? _carpetaDestinoFinal;

    private int _nivelesTotales;
    private int _nivelActual;
    private string _carpetaNavegacionActual = string.Empty;

    public IdentificarSinTextoWindow(string rutaArchivo)
    {
        InitializeComponent();
        _rutaArchivo = rutaArchivo;

        Visor.CargarPdf(rutaArchivo);
        ControlOrganizacion.ConfigurarProveedorDeFecha(LeerFechaReferencia);

        MostrarPanel(PanelElegir);
    }

    private void MostrarPanel(FrameworkElement panel)
    {
        foreach (var p in new FrameworkElement[] { PanelElegir, PanelCarpetaMadre, PanelOrganizacion, PanelVerUbicaciones, PanelNavegar, PanelNombreArchivo })
        {
            p.Visibility = p == panel ? Visibility.Visible : Visibility.Collapsed;
        }

        TxtError.Visibility = Visibility.Collapsed;
    }

    // ----- 3a: Crear ubicación nueva -----

    private void BtnCrearUbicacionNueva_Click(object sender, RoutedEventArgs e) => MostrarPanel(PanelCarpetaMadre);

    private void BtnElegirCarpetaMadre_Click(object sender, RoutedEventArgs e)
    {
        using var dialogo = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Elegir la carpeta madre: la raíz donde va a vivir todo lo de este tipo de documento"
        };

        if (dialogo.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        _carpetaMadre = dialogo.SelectedPath;
        TxtCarpetaMadre.Text = _carpetaMadre;

        var tieneSubcarpetas = Directory.GetDirectories(_carpetaMadre).Length > 0;
        if (!tieneSubcarpetas)
        {
            var directo = System.Windows.MessageBox.Show(
                this,
                "Esta carpeta madre está vacía. ¿Guardar el documento directo ahí, sin subcarpetas?",
                "Archivero", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (directo == MessageBoxResult.Yes)
            {
                _carpetaDestinoFinal = _carpetaMadre;
                _ubicaciones.ObtenerOCrear(_carpetaMadre, FormatoCarpeta.Directo, null);
                IrANombreArchivo();
                return;
            }
        }

        ControlOrganizacion.Iniciar(null, null, bloqueado: false);
        ActualizarVisibilidadMarcarFecha();
        MostrarPanel(PanelOrganizacion);
    }

    private void ControlOrganizacion_SeleccionCambiada() => ActualizarVisibilidadMarcarFecha();

    private void ActualizarVisibilidadMarcarFecha()
    {
        PanelMarcarFecha.Visibility = ControlOrganizacion.FechaEsAplicable ? Visibility.Visible : Visibility.Collapsed;
        TxtFechaOpcional.Visibility = ControlOrganizacion.FechaEsOpcional ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TxtFechaManual_TextChanged(object sender, TextChangedEventArgs e) =>
        ControlOrganizacion.RefrescarPorCambioDeFecha();

    private (DateTime Fecha, bool EsSupuesta) LeerFechaReferencia()
    {
        if (!string.IsNullOrWhiteSpace(TxtFechaManual.Text) && FechaExtraidaService.TryParsear(TxtFechaManual.Text, out var fecha))
        {
            return (fecha, false);
        }

        return (DateTime.Now, true);
    }

    private void BtnContinuarOrganizacion_Click(object sender, RoutedEventArgs e)
    {
        if (!ControlOrganizacion.Validar(out var error))
        {
            MostrarError(error!);
            return;
        }

        var formato = ControlOrganizacion.FormatoElegido!.Value;
        var patron = formato == FormatoCarpeta.Directo ? null : ControlOrganizacion.PatronElegido;

        if (ControlOrganizacion.FechaEsAplicable && !ControlOrganizacion.FechaEsOpcional
            && string.IsNullOrWhiteSpace(TxtFechaManual.Text))
        {
            MostrarError("Escribir la fecha del documento.");
            return;
        }

        var (fecha, _) = LeerFechaReferencia();
        var subcarpeta = FormatoCarpetaService.ConstruirSubcarpeta(formato, patron, fecha);
        _carpetaDestinoFinal = string.IsNullOrEmpty(subcarpeta) ? _carpetaMadre : Path.Combine(_carpetaMadre, subcarpeta);

        _ubicaciones.ObtenerOCrear(_carpetaMadre, formato, patron);

        IrANombreArchivo();
    }

    // ----- 3b: Ver ubicaciones disponibles -----

    private void BtnVerUbicaciones_Click(object sender, RoutedEventArgs e)
    {
        var ubicaciones = _ubicaciones.ObtenerTodas();
        TxtSinUbicaciones.Visibility = ubicaciones.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListaUbicaciones.ItemsSource = ubicaciones.Select(u => new FilaUbicacion(u)).ToList();
        MostrarPanel(PanelVerUbicaciones);
    }

    private void BtnVolverAElegir_Click(object sender, RoutedEventArgs e) => MostrarPanel(PanelElegir);

    private void ListaUbicaciones_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ListaUbicaciones.SelectedItem is not FilaUbicacion fila)
        {
            return;
        }

        var ubicacion = fila.Ubicacion;

        if (ubicacion.Formato == FormatoCarpeta.Directo)
        {
            var confirmar = System.Windows.MessageBox.Show(
                this, $"¿Guardar este documento en:\n{ubicacion.CarpetaMadre}?",
                "Archivero", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                _carpetaDestinoFinal = ubicacion.CarpetaMadre;
                IrANombreArchivo();
            }

            return;
        }

        _nivelesTotales = ubicacion.Patron!.Split('\\').Length;
        _nivelActual = 0;
        _carpetaNavegacionActual = ubicacion.CarpetaMadre;
        MostrarNivelNavegacion();
    }

    /// <summary>
    /// Navegación por niveles dentro de una ubicación organizada (Caso-4, punto 3b): solo lista
    /// subcarpetas ya existentes, nivel por nivel -- no hace falta más que eso para ahorrarle al
    /// usuario el paso de navegar a mano por el explorador de Windows.
    /// </summary>
    private void MostrarNivelNavegacion()
    {
        TxtRutaNavegacion.Text = _carpetaNavegacionActual;

        var subcarpetas = Directory.GetDirectories(_carpetaNavegacionActual)
            .Select(Path.GetFileName)
            .OrderDescending(StringComparer.OrdinalIgnoreCase)
            .ToList();

        TxtSinSubcarpetas.Visibility = subcarpetas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListaNavegacion.ItemsSource = subcarpetas;
        MostrarPanel(PanelNavegar);
    }

    private void ListaNavegacion_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ListaNavegacion.SelectedItem is not string nombreCarpeta)
        {
            return;
        }

        _carpetaNavegacionActual = Path.Combine(_carpetaNavegacionActual, nombreCarpeta);
        _nivelActual++;

        if (_nivelActual >= _nivelesTotales)
        {
            _carpetaDestinoFinal = _carpetaNavegacionActual;
            IrANombreArchivo();
            return;
        }

        MostrarNivelNavegacion();
    }

    private void BtnVolverNavegacion_Click(object sender, RoutedEventArgs e) => MostrarPanel(PanelVerUbicaciones);

    // ----- Paso final: nombre de archivo y guardado -----

    private void IrANombreArchivo()
    {
        TxtCarpetaDestinoFinal.Text = $"Se va a guardar en: {_carpetaDestinoFinal}";
        TxtNombreArchivo.Text = Path.GetFileNameWithoutExtension(_rutaArchivo);
        MostrarPanel(PanelNombreArchivo);
    }

    private void BtnBorrarNombre_Click(object sender, RoutedEventArgs e) => TxtNombreArchivo.Clear();

    private void BtnSoloNumeros_Click(object sender, RoutedEventArgs e)
    {
        TxtNombreArchivo.Text = new string(TxtNombreArchivo.Text.Where(char.IsDigit).ToArray());
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var nombre = TxtNombreArchivo.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarError("Escribir un nombre de archivo.");
            return;
        }

        try
        {
            // Para este punto _carpetaDestinoFinal ya es la carpeta exacta (con cualquier
            // subcarpeta de fecha ya resuelta, o la carpeta navegada a mano): se guarda
            // "Directo" ahí, renombrando siempre al nombre que el usuario dejó en este paso.
            var configuracionTemporal = new ConfiguracionDocumento
            {
                Emisor = "(sin texto)",
                Tipo = "(sin texto)",
                CarpetaDestino = _carpetaDestinoFinal!,
                FormatoCarpeta = FormatoCarpeta.Directo,
                PatronCarpeta = null,
                Renombrar = true,
                Patrones = []
            };

            string rutaFinal;
            try
            {
                rutaFinal = ClasificadorService.Clasificar(_rutaArchivo, configuracionTemporal, null, nombre);
            }
            catch (ArchivoDuplicadoException ex)
            {
                var resolver = new ResolverDuplicadoWindow(_rutaArchivo, ex.RutaDestino) { Owner = this };
                if (resolver.ShowDialog() != true)
                {
                    MostrarError("Documento dejado pendiente por nombre duplicado. Podés intentar de nuevo o cerrar.");
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

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
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
