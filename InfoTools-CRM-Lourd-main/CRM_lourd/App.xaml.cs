using System.Configuration;
using System.Data;
using System.Windows;

namespace CRM_lourd
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Au lieu de lancer MainWindow, on lance LoginWindow
            LoginWindow login = new LoginWindow();
            login.Show();
        }
    }
}