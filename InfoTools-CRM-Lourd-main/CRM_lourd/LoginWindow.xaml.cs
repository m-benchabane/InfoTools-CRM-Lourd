using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace CRM_lourd
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string u = txtUser.Text;
            string p = txtPass.Password;

            // --- EN ATTENDANT L'AD ---
            // Si l'utilisateur tape "admin", on le connecte en tant que Manager
            if (u == "admin" && p == "admin")
            {
                Session.CurrentUser = "Admin";
                Session.Role = "Manager"; // On définit le rôle
                OpenMainWindow();
            }
            // Si l'utilisateur tape "user", on le connecte en tant que Vendeur
            else if (u == "user" && p == "user")
            {
                Session.Role = "Vendeur";
                OpenMainWindow();
            }
            else
            {
                MessageBox.Show("Erreur : Identifiants incorrects (Essayez admin/admin)");
            }
        }

        private void OpenMainWindow()
        {
            // On ouvre la vraie application
            MainWindow main = new MainWindow();
            main.Show();

            // On ferme la fenêtre de login
            this.Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

