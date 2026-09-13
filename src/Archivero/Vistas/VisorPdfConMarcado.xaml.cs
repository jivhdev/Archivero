using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Archivero.Servicios.Pdf;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using UserControl = System.Windows.Controls.UserControl;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using Canvas = System.Windows.Controls.Canvas;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Archivero.Vistas;

public partial class VisorPdfConMarcado : UserControl
{
    private string _rutaPdf = string.Empty;
    private int _paginaActual;
    private int _totalPaginas = 1;
    private int _anchoPaginaActual;
    private int _altoPaginaActual;
    private Point? _inicioArrastre;
    private Rectangle? _rectanguloArrastre;

    public event Action<int, RectanguloFraccion>? MarcaRealizada;

    public VisorPdfConMarcado()
    {
        InitializeComponent();
    }

    public void CargarPdf(string rutaPdf)
    {
        _rutaPdf = rutaPdf;
        _paginaActual = 0;
        _totalPaginas = Math.Max(1, LectorPdf.ContarPaginas(rutaPdf));
        MostrarPaginaActual();
    }

    private void MostrarPaginaActual()
    {
        var pagina = LectorPdf.RenderizarPagina(_rutaPdf, _paginaActual);
        _anchoPaginaActual = pagina.Ancho;
        _altoPaginaActual = pagina.Alto;

        var bitmap = BitmapSource.Create(
            pagina.Ancho, pagina.Alto, 96, 96, PixelFormats.Bgra32, null, pagina.PixelesBgra, pagina.Ancho * 4);
        bitmap.Freeze();

        ImagenPagina.Source = bitmap;
        TxtPagina.Text = $"Página {_paginaActual + 1} de {_totalPaginas}";
        BtnAnterior.IsEnabled = _paginaActual > 0;
        BtnSiguiente.IsEnabled = _paginaActual < _totalPaginas - 1;

        CapaMarcas.Children.Clear();
    }

    private void BtnAnterior_Click(object sender, RoutedEventArgs e)
    {
        if (_paginaActual > 0)
        {
            _paginaActual--;
            MostrarPaginaActual();
        }
    }

    private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
    {
        if (_paginaActual < _totalPaginas - 1)
        {
            _paginaActual++;
            MostrarPaginaActual();
        }
    }

    private void ImagenPagina_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ImagenPagina.Source is null)
        {
            return;
        }

        _inicioArrastre = e.GetPosition(ImagenPagina);
        ImagenPagina.CaptureMouse();

        _rectanguloArrastre = new Rectangle
        {
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(60, 30, 144, 255))
        };
        CapaMarcas.Children.Add(_rectanguloArrastre);
    }

    private void ImagenPagina_MouseMove(object sender, MouseEventArgs e)
    {
        if (_inicioArrastre is not { } inicio || _rectanguloArrastre is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var actual = e.GetPosition(ImagenPagina);
        var x = Math.Min(inicio.X, actual.X);
        var y = Math.Min(inicio.Y, actual.Y);
        var ancho = Math.Abs(actual.X - inicio.X);
        var alto = Math.Abs(actual.Y - inicio.Y);

        Canvas.SetLeft(_rectanguloArrastre, x);
        Canvas.SetTop(_rectanguloArrastre, y);
        _rectanguloArrastre.Width = ancho;
        _rectanguloArrastre.Height = alto;
    }

    private void ImagenPagina_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_inicioArrastre is not { } inicio)
        {
            return;
        }

        ImagenPagina.ReleaseMouseCapture();
        var fin = e.GetPosition(ImagenPagina);
        _inicioArrastre = null;

        if (Math.Abs(fin.X - inicio.X) < 4 || Math.Abs(fin.Y - inicio.Y) < 4)
        {
            if (_rectanguloArrastre is not null)
            {
                CapaMarcas.Children.Remove(_rectanguloArrastre);
            }
            return;
        }

        var fraccion = ConvertirAFraccion(inicio, fin);
        if (fraccion is not null)
        {
            MarcaRealizada?.Invoke(_paginaActual, fraccion);
        }
    }

    private Rect ObtenerRectanguloImagenEnControl()
    {
        var controlAncho = ImagenPagina.ActualWidth;
        var controlAlto = ImagenPagina.ActualHeight;
        if (controlAncho <= 0 || controlAlto <= 0 || _anchoPaginaActual <= 0 || _altoPaginaActual <= 0)
        {
            return new Rect(0, 0, controlAncho, controlAlto);
        }

        var escalaControl = controlAncho / controlAlto;
        var escalaImagen = (double)_anchoPaginaActual / _altoPaginaActual;

        double anchoMostrado, altoMostrado;
        if (escalaImagen > escalaControl)
        {
            anchoMostrado = controlAncho;
            altoMostrado = controlAncho / escalaImagen;
        }
        else
        {
            altoMostrado = controlAlto;
            anchoMostrado = controlAlto * escalaImagen;
        }

        var x = (controlAncho - anchoMostrado) / 2;
        var y = (controlAlto - altoMostrado) / 2;

        return new Rect(x, y, anchoMostrado, altoMostrado);
    }

    private RectanguloFraccion? ConvertirAFraccion(Point inicio, Point fin)
    {
        var rectImagen = ObtenerRectanguloImagenEnControl();
        if (rectImagen.Width <= 0 || rectImagen.Height <= 0)
        {
            return null;
        }

        double NormalizarX(double x) => Math.Clamp((x - rectImagen.X) / rectImagen.Width, 0, 1);
        double NormalizarY(double y) => Math.Clamp((y - rectImagen.Y) / rectImagen.Height, 0, 1);

        var x0 = NormalizarX(Math.Min(inicio.X, fin.X));
        var x1 = NormalizarX(Math.Max(inicio.X, fin.X));
        var y0 = NormalizarY(Math.Min(inicio.Y, fin.Y));
        var y1 = NormalizarY(Math.Max(inicio.Y, fin.Y));

        return new RectanguloFraccion(x0, y0, x1 - x0, y1 - y0);
    }
}
