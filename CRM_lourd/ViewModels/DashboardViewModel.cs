using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
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
    public class ClientDashboard
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string Initial => !string.IsNullOrEmpty(Name) ? Name.Substring(0, 1).ToUpper() : "?";
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _clientsCount;
        private int _upcomingAppointmentsCount;
        private int _monthlyInvoicesCount;
        private int _productsCount;

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
            QuestPDF.Settings.License = LicenseType.Community;

            ExportPdfCommand = new RelayCommand(param => ExecuteExportPdf());
            RefreshCommand = new RelayCommand(param => LoadData());

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
                    // KPI — clients actifs uniquement
                    ClientsCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM customers WHERE status = 'actif'", conn).ExecuteScalar());
                    UpcomingAppointmentsCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM appointments WHERE start_at >= NOW()", conn).ExecuteScalar());
                    MonthlyInvoicesCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM invoices WHERE MONTH(invoiced_at)=MONTH(CURDATE())", conn).ExecuteScalar());
                    ProductsCount = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM products", conn).ExecuteScalar());

                    // Derniers clients actifs avec téléphone
                    RecentClients.Clear();
                    using (var reader = new MySqlCommand(
                        "SELECT id, name, email, phone FROM customers WHERE status = 'actif' ORDER BY id DESC LIMIT 5", conn).ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            RecentClients.Add(new ClientDashboard
                            {
                                Id = reader.GetInt64(0),
                                Name = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                                Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Phone = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }
                UpdateChart();
            }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); }
        }

        private void UpdateChart()
        {
            var values = new ChartValues<int> { ClientsCount, UpcomingAppointmentsCount, MonthlyInvoicesCount, ProductsCount };
            if (StatsSeries.Count == 0)
            {
                StatsSeries.Add(new ColumnSeries
                {
                    Title = "Indicateurs",
                    Values = values,
                    Fill = new WpfMedia.SolidColorBrush((WpfMedia.Color)WpfMedia.ColorConverter.ConvertFromString("#1E40AF"))
                });
            }
            else { StatsSeries[0].Values = values; }
        }

        private void ExecuteExportPdf()
        {
            try
            {
                string folderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InfoTools_Exports");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string fileName = $"Rapport_Activite_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                string filePath = System.IO.Path.Combine(folderPath, fileName);

                QuestPDF.Fluent.Document.Create(docContainer =>
                {
                    docContainer.Page(page =>
                    {
                        page.Margin(40);
                        page.Background(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontColor("#1e293b"));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("INFOTOOLS").FontSize(22).ExtraBold().FontColor("#1E40AF");
                                col.Item().Text("SOLUTION DE GESTION CRM").FontSize(9).SemiBold().FontColor("#64748b");
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("RAPPORT ANALYTIQUE").FontSize(14).SemiBold();
                                col.Item().Text($"{DateTime.Now:dd MMMM yyyy}").FontSize(10);
                            });
                        });

                        page.Content().PaddingVertical(25).Column(col =>
                        {
                            col.Spacing(20);

                            col.Item().Grid(grid =>
                            {
                                grid.Columns(4);
                                grid.Spacing(12);
                                grid.Item().Element(c => ModernStatCard(c, "CLIENTS ACTIFS", ClientsCount, "#3B82F6"));
                                grid.Item().Element(c => ModernStatCard(c, "RDV PREVUS", UpcomingAppointmentsCount, "#10B981"));
                                grid.Item().Element(c => ModernStatCard(c, "FACTURES (MOIS)", MonthlyInvoicesCount, "#F59E0B"));
                                grid.Item().Element(c => ModernStatCard(c, "CATALOGUE", ProductsCount, "#8B5CF6"));
                            });

                            col.Item().PaddingTop(10).Column(tableCol =>
                            {
                                tableCol.Spacing(8);
                                tableCol.Item().Text("DERNIERS CLIENTS ACTIFS").FontSize(12).SemiBold().FontColor("#1E40AF");
                                tableCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(40);
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });
                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(1).BorderColor("#cbd5e1").PaddingVertical(5).Text("ID").SemiBold();
                                        header.Cell().BorderBottom(1).BorderColor("#cbd5e1").PaddingVertical(5).Text("NOM").SemiBold();
                                        header.Cell().BorderBottom(1).BorderColor("#cbd5e1").PaddingVertical(5).Text("EMAIL").SemiBold();
                                        header.Cell().BorderBottom(1).BorderColor("#cbd5e1").PaddingVertical(5).Text("TELEPHONE").SemiBold();
                                    });
                                    foreach (var client in RecentClients)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor("#f1f5f9").PaddingVertical(5).Text(client.Id.ToString());
                                        table.Cell().BorderBottom(1).BorderColor("#f1f5f9").PaddingVertical(5).Text(client.Name);
                                        table.Cell().BorderBottom(1).BorderColor("#f1f5f9").PaddingVertical(5).Text(client.Email);
                                        table.Cell().BorderBottom(1).BorderColor("#f1f5f9").PaddingVertical(5).Text(client.Phone);
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Column(fCol =>
                        {
                            fCol.Item().LineHorizontal(1).LineColor("#f1f5f9");
                            fCol.Item().PaddingTop(5).Text(x =>
                            {
                                x.Span("InfoTools CRM - Rapport genere automatiquement - Page ");
                                x.CurrentPageNumber();
                            });
                        });
                    });
                }).GeneratePdf(filePath);

                MessageBox.Show($"Export reussi !\nEmplacement : {folderPath}", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start("explorer.exe", folderPath);
            }
            catch (Exception ex) { MessageBox.Show("Erreur export PDF : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ModernStatCard(QuestPDF.Infrastructure.IContainer container, string title, int val, string accentColor)
        {
            container
                .BorderLeft(3).BorderColor(accentColor)
                .Background("#f8fafc")
                .Padding(12)
                .Column(c =>
                {
                    c.Item().Text(title).FontSize(8).SemiBold().FontColor("#64748b");
                    c.Item().PaddingTop(2).Text(val.ToString()).FontSize(22).ExtraBold().FontColor(QuestPDF.Helpers.Colors.Black);
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