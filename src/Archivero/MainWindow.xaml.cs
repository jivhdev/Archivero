using System.Windows;

namespace Archivero;

public partial class MainWindow : Window
{
    public MainWindow(string carpetaObservada)
    {
        InitializeComponent();
        TxtCarpetaObservada.Text = $"Carpeta observada: {carpetaObservada}";
    }
}
