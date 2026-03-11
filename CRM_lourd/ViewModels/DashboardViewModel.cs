using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfMedia = System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CRM_lourd.ViewModels
{
    // Définition locale du modèle pour éviter l'erreur "Models n'existe pas"
    public class ClientDashboard
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _clientsCount;
        private int _upcomingAppointmentsCount;
        private int _monthlyInvoicesCount;
        private int _productsCount;

        // Propriétés avec notification UI
        public int ClientsCount { get => _clientsCount; set { _clientsCount = value; OnPropertyChanged(nameof(ClientsCount)); UpdateChart(); } }
        public int UpcomingAppointmentsCount { get => _upcomingAppointmentsCount; set { _upcomingAppointmentsCount = value; OnPropertyChanged(nameof(UpcomingAppointmentsCount)); UpdateChart(); } }
        public int MonthlyInvoicesCount { get => _monthlyInvoicesCount; set { _monthlyInvoicesCount = value; OnPropertyChanged(nameof(MonthlyInvoicesCount)); UpdateChart(); } }
        public int ProductsCount { get => _productsCount; set { _productsCount = value; OnPropertyChanged(nameof(ProductsCount)); UpdateChart(); } }

        public ObservableCollection<ClientDashboard> RecentClients { get; set; } = new ObservableCollection<ClientDashboard>();

        private SeriesCollection _statsSeries;
        public SeriesCollection StatsSeries { get => _statsSeries; set { _statsSeries = value; OnPropertyChanged(nameof(StatsSeries)); } }

        public List<string> StatsLabels { get; set; }

        public ICommand ExportPdfCommand { get; }
        public ICommand RefreshCommand { get; }

        public DashboardViewModel()
        {
            // Initialisation QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            ExportPdfCommand = new RelayCommand(param => ExecuteExportPdf());
            RefreshCommand = new RelayCommand(param => LoadData());

            // Initialisation du graphique avant les données
            StatsLabels = new List<string> { "Clients", "RDV", "Factures", "Produits" };
            StatsSeries = new SeriesCollection();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Database db = new Database();
                using (var conn = db.GetConnection())
                {
                    ClientsCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM customers", conn).ExecuteScalar());
                    UpcomingAppointmentsCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM appointments WHERE start_at >= NOW()", conn).ExecuteScalar());
                    MonthlyInvoicesCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM invoices WHERE MONTH(invoiced_at)=MONTH(CURDATE())", conn).ExecuteScalar());
                    ProductsCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM products", conn).ExecuteScalar());

                    RecentClients.Clear();
                    using (var reader = new MySqlCommand("SELECT id, name, email FROM customers ORDER BY id DESC LIMIT 5", conn).ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RecentClients.Add(new ClientDashboard
                            {
                                Id = reader.GetInt64(0),
                                Name = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                                Email = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
                UpdateChart();
            }
            catch (Exception ex) { MessageBox.Show("BDD Erreur : " + ex.Message); }
        }

        private void UpdateChart()
        {
            var values = new ChartValues<int> { ClientsCount, UpcomingAppointmentsCount, MonthlyInvoicesCount, ProductsCount };

            if (StatsSeries.Count == 0)
            {
                StatsSeries.Add(new ColumnSeries
                {
                    Title = "Stats",
                    Values = values,
                    Fill = new WpfMedia.SolidColorBrush((WpfMedia.Color)WpfMedia.ColorConverter.ConvertFromString("#2563EB"))
                });
            }
            else
            {
                StatsSeries[0].Values = values;
            }
        }

        private void ExecuteExportPdf()
        {
            try
            {
                string fileName = $"Rapport_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);
                        page.Header().Text("TABLEAU DE BORD").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Spacing(15);
                            col.Item().Grid(grid =>
                            {
                                grid.Columns(2);
                                grid.Spacing(10);
                                // On utilise le nom complet QuestPDF.Infrastructure.IContainer pour éviter l'erreur CS0104
                                grid.Item().Element(c => StatBlock(c, "Clients", ClientsCount));
                                grid.Item().Element(c => StatBlock(c, "RDV", UpcomingAppointmentsCount));
                                grid.Item().Element(c => StatBlock(c, "Factures", MonthlyInvoicesCount));
                                grid.Item().Element(c => StatBlock(c, "Produits", ProductsCount));
                            });
                        });
                    });
                }).GeneratePdf(fileName);

                MessageBox.Show("PDF généré !");
            }
            catch (Exception ex) { MessageBox.Show("Erreur PDF : " + ex.Message); }
        }

        // Correction de l'erreur CS0104 : On précise explicitement le namespace ici
        private void StatBlock(QuestPDF.Infrastructure.IContainer container, string title, int val)
        {
            container.Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
            {
                c.Item().Text(title).FontSize(10);
                c.Item().Text(val.ToString()).FontSize(16).Bold();
            });
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged;
    }
}