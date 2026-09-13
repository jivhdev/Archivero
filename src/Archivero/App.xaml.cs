using System.Windows;
using Archivero.Datos;
using Archivero.Servicios;
using Archivero.Vistas;

namespace Archivero;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        BaseDeDatos.AsegurarEsquema();

        var configuracion = new ConfiguracionRepository();
        var servicioCarpeta = new CarpetaObservadaService(configuracion);

        var carpetaObservada = servicioCarpeta.ObtenerCarpetaConfigurada();

        if (string.IsNullOrEmpty(carpetaObservada))
        {
            var onboarding = new OnboardingWindow(servicioCarpeta);
            var confirmado = onboarding.ShowDialog();

            if (confirmado != true || onboarding.CarpetaCreada is null)
            {
                Shutdown();
                return;
            }

            carpetaObservada = onboarding.CarpetaCreada;
        }

        var ventanaPrincipal = new MainWindow(carpetaObservada);
        MainWindow = ventanaPrincipal;
        ventanaPrincipal.Show();
    }
}
