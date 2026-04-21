using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Nécessaire pour les couleurs
using CRM_lourd.Views;

namespace CRM_lourd
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 1. Ouvre le Dashboard au démarrage
            MainContent.Content = new DashboardView();

            // 2. AFFICHER LE PROFIL UTILISATEUR (NOUVEAU CODE)
            UpdateUserProfile();
        }

        private void UpdateUserProfile()
        {
            // On récupère les infos depuis la classe statique Session
            // (Assurez-vous que Session.CurrentUser et Session.Role existent bien comme vu précédemment)
            string currentUser = Session.CurrentUser ?? "Invité";
            string currentRole = Session.Role ?? "Utilisateur";

            // Mise à jour du nom et du rôle
            txtUserName.Text = currentUser.ToUpper(); // Nom en majuscules
            txtUserRole.Text = currentRole;

            // Mise à jour de l'initiale (Première lettre du nom)
            if (!string.IsNullOrEmpty(currentUser))
            {
                txtUserInitial.Text = currentUser.Substring(0, 1).ToUpper();
            }

            // Petit bonus visuel : Si c'est un Manager, on écrit le rôle en vert clair
            if (Session.IsManager)
            {
                txtUserRole.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)); // Vert clair (#4ADE80)
                txtUserRole.FontWeight = FontWeights.Bold;
            }
        }

        // Gestion du clic sur les boutons du menu
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Charge le UserControl correspondant
            switch (button.Tag.ToString())
            {
                case "Dashboard":
                    MainContent.Content = new DashboardView();
                    break;

                case "Clients":
                    MainContent.Content = new ClientsView();
                    break;

                case "Appointments":
                    MainContent.Content = new AppointmentsView();
                    break;

                case "Products":
                    MainContent.Content = new ProductsView();
                    break;

                case "Invoices":
                    MainContent.Content = new InvoicesView();
                    break;
            }
        }
    }
}