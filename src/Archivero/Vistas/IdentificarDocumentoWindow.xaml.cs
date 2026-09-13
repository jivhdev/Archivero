using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Archivero.Datos;
using Archivero.Servicios;
using Archivero.Servicios.Pdf;

namespace Archivero.Vistas;

public partial class IdentificarDocumentoWindow : Window
{
    private enum Paso { EmisorTipo, Carpeta, Formato, NombreArchivo, Confirmar }

    private readonly string _rutaArchivo;
    private readonly EntidadRepository _entidades = new();
    private readonly ConfiguracionDocumentoRepository _configuraciones = new();
    private readonly PendienteRepository _pendientes = new();
    private readonly Dictionary<CampoMarca, Marca> _marcas = new();

    private Paso _paso;
    private CampoMarca? _campoActivoParaMarcar;

    private string _emisor = string.Empty;
    private string _tipo = string.Empty;
    private string _carpetaDestino = string.Empty;
    private FormatoCarpeta _formato;
    private string? _patronCarpeta;
    private bool _renombrar;

    public IdentificarDocumentoWindow(string rutaArchivo)
    {
        InitializeComponent();
        _rutaArchivo = rutaArchivo;

        Visor.CargarPdf(rutaArchivo);
        Visor.MarcaRealizada += Visor_MarcaRealizada;

        CmbEmisor.ItemsSource = _entidades.Buscar(CategoriaEntidad.Emisor, string.Empty);
        CmbTipo.ItemsSource = _entidades.Buscar(CategoriaEntidad.Tipo, string.Empty);

        ActualizarEstadosDeMarca();
        MostrarPaso(Paso.EmisorTipo);
    }

    private void MostrarPaso(Paso nuevoPaso)
    {
        _paso = nuevoPaso;

        PanelEmisorTipo.Visibility = nuevoPaso == Paso.EmisorTipo ? Visibility.Visible : Visibility.Collapsed;
        PanelCarpeta.Visibility = nuevoPaso == Paso.Carpeta ? Visibility.Visible : Visibility.Collapsed;
        PanelFormato.Visibility = nuevoPaso == Paso.Formato ? Visibility.Visible : Visibility.Collapsed;
        PanelNombreArchivo.Visibility = nuevoPaso == Paso.NombreArchivo ? Visibility.Visible : Visibility.Collapsed;
        PanelConfirmar.Visibility = nuevoPaso == Paso.Confirmar ? Visibility.Visible : Visibility.Collapsed;

        TxtError.Visibility = Visibility.Collapsed;

        (TxtTituloPaso.Text, TxtInstruccionPaso.Text) = nuevoPaso switch
        {
            Paso.EmisorTipo => ("Paso 1 de 5 — Emisor y Tipo",
                "Marcá sobre el PDF dónde aparecen el Emisor y el Tipo de documento, y escribilos (o elegí uno ya conocido)."),
            Paso.Carpeta => ("Paso 2 de 5 — Carpeta de destino",
                "Elegí en qué carpeta se van a guardar los documentos de este Emisor y Tipo."),
            Paso.Formato => ("Paso 3 de 5 — Formato de subcarpetas",
                "Confirmá o corregí cómo se organizan las subcarpetas de fecha."),
            Paso.NombreArchivo => ("Paso 4 de 5 — Nombre de archivo",
                "Elegí cómo se va a llamar el archivo guardado."),
            Paso.Confirmar => ("Paso 5 de 5 — Confirmar", "Revisá los datos antes de guardar."),
            _ => (string.Empty, string.Empty)
        };

        BtnSiguiente.Content = nuevoPaso == Paso.Confirmar ? "Guardar y clasificar" : "Siguiente";
    }

    private void BtnMarcarEmisor_Click(object sender, RoutedEventArgs e) => ArmarMarca(CampoMarca.Emisor);

    private void BtnMarcarTipo_Click(object sender, RoutedEventArgs e) => ArmarMarca(CampoMarca.Tipo);

    private void BtnMarcarFecha_Click(object sender, RoutedEventArgs e) => ArmarMarca(CampoMarca.Fecha);

    private void BtnMarcarNombreArchivo_Click(object sender, RoutedEventArgs e) => ArmarMarca(CampoMarca.NombreArchivo);

    private void ArmarMarca(CampoMarca campo)
    {
        _campoActivoParaMarcar = campo;
        TxtInstruccionPaso.Text = $"Dibujá un rectángulo sobre el PDF donde aparece: {NombreCampo(campo)}.";
    }

    private static string NombreCampo(CampoMarca campo) => campo switch
    {
        CampoMarca.Emisor => "Emisor",
        CampoMarca.Tipo => "Tipo de documento",
        CampoMarca.Fecha => "Fecha",
        CampoMarca.NombreArchivo => "Campo para el nombre de archivo",
        _ => campo.ToString()
    };

    private void Visor_MarcaRealizada(int pagina, RectanguloFraccion fraccion)
    {
        if (_campoActivoParaMarcar is not { } campo)
        {
            return;
        }

        _marcas[campo] = new Marca(campo, pagina, fraccion.X, fraccion.Y, fraccion.Ancho, fraccion.Alto);
        _campoActivoParaMarcar = null;
        ActualizarEstadosDeMarca();
    }

    private void ActualizarEstadosDeMarca()
    {
        TxtEstadoMarcaEmisor.Text = EstadoTexto(CampoMarca.Emisor);
        TxtEstadoMarcaTipo.Text = EstadoTexto(CampoMarca.Tipo);
        TxtEstadoMarcaFecha.Text = EstadoTexto(CampoMarca.Fecha);
        TxtEstadoMarcaNombre.Text = EstadoTexto(CampoMarca.NombreArchivo);
    }

    private string EstadoTexto(CampoMarca campo) =>
        _marcas.TryGetValue(campo, out var marca) ? $"Marca hecha (página {marca.Pagina + 1})." : "Todavía no marcado.";

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
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Elegí la carpeta de destino para este Emisor y Tipo"
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
        PanelMarcarFecha.Visibility = esDirecto ? Visibility.Collapsed : Visibility.Visible;

        if (esDirecto)
        {
            return;
        }

        var conocidos = _configuraciones.ObtenerPatronesDeCarpetaConocidos();
        var presets = esAnioMes ? new[] { "yyyy\\MM", "yyyy\\MMMM" } : new[] { "yyyy", "yy" };
        var opciones = presets.Concat(conocidos.Where(p => esAnioMes == p.Contains('\\'))).Distinct().ToList();

        CmbPatronCarpeta.ItemsSource = opciones;
        if (string.IsNullOrWhiteSpace(CmbPatronCarpeta.Text) && opciones.Count > 0)
        {
            CmbPatronCarpeta.SelectedIndex = 0;
        }
    }

    private void OpcionNombre_Changed(object sender, RoutedEventArgs e)
    {
        PanelMarcarNombre.Visibility = RbExtraerNombre.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PrepararPasoFormato(DeteccionFormatoCarpeta deteccion)
    {
        RbDirecto.IsChecked = deteccion.Formato == FormatoCarpeta.Directo;
        RbAnio.IsChecked = deteccion.Formato == FormatoCarpeta.Anio;
        RbAnioMes.IsChecked = deteccion.Formato == FormatoCarpeta.AnioMes;

        if (deteccion.PatronCarpeta is not null)
        {
            CmbPatronCarpeta.Text = deteccion.PatronCarpeta;
        }
    }

    private void MostrarResumen()
    {
        var formatoTexto = _formato switch
        {
            FormatoCarpeta.Directo => "directo en la carpeta",
            FormatoCarpeta.Anio => $"por año ({_patronCarpeta})",
            FormatoCarpeta.AnioMes => $"por año y mes ({_patronCarpeta})",
            _ => string.Empty
        };
        var nombreTexto = _renombrar ? "se extrae del campo marcado en el PDF" : "se mantiene el nombre original";

        TxtResumen.Text =
            $"Emisor: {_emisor}\n" +
            $"Tipo: {_tipo}\n" +
            $"Carpeta destino: {_carpetaDestino}\n" +
            $"Subcarpetas: {formatoTexto}\n" +
            $"Nombre de archivo: {nombreTexto}";
    }

    private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
    {
        switch (_paso)
        {
            case Paso.EmisorTipo:
                _emisor = CmbEmisor.Text.Trim();
                _tipo = CmbTipo.Text.Trim();
                if (string.IsNullOrWhiteSpace(_emisor) || string.IsNullOrWhiteSpace(_tipo))
                {
                    MostrarError("Completá el Emisor y el Tipo.");
                    return;
                }

                if (!_marcas.ContainsKey(CampoMarca.Emisor) || !_marcas.ContainsKey(CampoMarca.Tipo))
                {
                    MostrarError("Marcá el Emisor y el Tipo sobre el PDF.");
                    return;
                }

                MostrarPaso(Paso.Carpeta);
                break;

            case Paso.Carpeta:
                if (string.IsNullOrWhiteSpace(TxtCarpetaDestino.Text))
                {
                    MostrarError("Elegí una carpeta de destino.");
                    return;
                }

                _carpetaDestino = TxtCarpetaDestino.Text;
                PrepararPasoFormato(FormatoCarpetaService.Detectar(_carpetaDestino));
                MostrarPaso(Paso.Formato);
                break;

            case Paso.Formato:
                if (RbDirecto.IsChecked != true && RbAnio.IsChecked != true && RbAnioMes.IsChecked != true)
                {
                    MostrarError("Elegí un formato de carpeta.");
                    return;
                }

                _formato = RbDirecto.IsChecked == true
                    ? FormatoCarpeta.Directo
                    : RbAnio.IsChecked == true ? FormatoCarpeta.Anio : FormatoCarpeta.AnioMes;

                if (_formato == FormatoCarpeta.Directo)
                {
                    _patronCarpeta = null;
                }
                else
                {
                    _patronCarpeta = CmbPatronCarpeta.Text.Trim();
                    if (string.IsNullOrWhiteSpace(_patronCarpeta))
                    {
                        MostrarError("Definí el patrón de la subcarpeta (por ejemplo: yyyy).");
                        return;
                    }

                    if (!_marcas.ContainsKey(CampoMarca.Fecha))
                    {
                        MostrarError("Marcá dónde aparece la fecha en el PDF.");
                        return;
                    }
                }

                MostrarPaso(Paso.NombreArchivo);
                break;

            case Paso.NombreArchivo:
                if (RbMantenerNombre.IsChecked != true && RbExtraerNombre.IsChecked != true)
                {
                    MostrarError("Elegí cómo se va a llamar el archivo.");
                    return;
                }

                _renombrar = RbExtraerNombre.IsChecked == true;
                if (_renombrar && !_marcas.ContainsKey(CampoMarca.NombreArchivo))
                {
                    MostrarError("Marcá en el PDF el campo que se va a usar como nombre de archivo.");
                    return;
                }

                MostrarResumen();
                MostrarPaso(Paso.Confirmar);
                break;

            case Paso.Confirmar:
                GuardarYClasificar();
                break;
        }
    }

    private void GuardarYClasificar()
    {
        try
        {
            var marcas = _marcas.Values.ToList();

            _configuraciones.GuardarNueva(_emisor, _tipo, _carpetaDestino, _formato, _patronCarpeta, _renombrar, marcas);

            DateTime? fecha = null;
            if (_marcas.TryGetValue(CampoMarca.Fecha, out var marcaFecha))
            {
                var textoFecha = LectorPdf.ExtraerTexto(
                    _rutaArchivo, marcaFecha.Pagina,
                    new RectanguloFraccion(marcaFecha.X, marcaFecha.Y, marcaFecha.Ancho, marcaFecha.Alto));

                if (!DateTime.TryParse(textoFecha, CultureInfo.GetCultureInfo("es-AR"), DateTimeStyles.None, out var fechaParseada))
                {
                    MostrarError($"No se pudo interpretar la fecha extraída (\"{textoFecha}\"). Revisá la marca sobre el PDF.");
                    return;
                }

                fecha = fechaParseada;
            }

            string? nombreExtraido = null;
            if (_renombrar && _marcas.TryGetValue(CampoMarca.NombreArchivo, out var marcaNombre))
            {
                nombreExtraido = LectorPdf.ExtraerTexto(
                    _rutaArchivo, marcaNombre.Pagina,
                    new RectanguloFraccion(marcaNombre.X, marcaNombre.Y, marcaNombre.Ancho, marcaNombre.Alto));

                if (string.IsNullOrWhiteSpace(nombreExtraido))
                {
                    MostrarError("No se pudo extraer un nombre válido de la coordenada marcada.");
                    return;
                }
            }

            var configuracion = _configuraciones.BuscarPorEmisorYTipo(_emisor, _tipo)!;
            var rutaFinal = ClasificadorService.Clasificar(_rutaArchivo, configuracion, fecha, nombreExtraido);

            _pendientes.Quitar(_rutaArchivo);

            System.Windows.MessageBox.Show(
                this, $"Documento guardado en:\n{rutaFinal}", "Archivero",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (ArchivoDuplicadoException ex)
        {
            MostrarError($"{ex.Message} Por ahora Archivero no reemplaza duplicados automáticamente desde este asistente.");
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo guardar: {ex.Message}");
        }
    }

    private void BtnPosponer_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        var confirmar = System.Windows.MessageBox.Show(
            this,
            "¿Cancelar la identificación de este documento? Se pierde lo marcado hasta ahora; el archivo sigue en pendientes.",
            "Archivero", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmar == MessageBoxResult.Yes)
        {
            DialogResult = false;
            Close();
        }
    }

    private void MostrarError(string mensaje)
    {
        TxtError.Text = mensaje;
        TxtError.Visibility = Visibility.Visible;
    }
}
