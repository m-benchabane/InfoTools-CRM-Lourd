using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using CRM_lourd;

namespace CRM_lourd.Views
{
    // Petit modèle interne pour les commerciaux
    public class Commercial
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public partial class AppointmentsView : UserControl
    {
        private string _currentTab = "actif";
        private List<Appointment> _allAppointments = new List<Appointment>();

        public AppointmentsView()
        {
            InitializeComponent();
            LoadClients();

            // Afficher le sélecteur de commercial uniquement pour les Managers
            if (Session.IsManager)
            {
                pnlCommercial.Visibility = Visibility.Visible;
                LoadCommerciaux();
            }
            else
            {
                pnlCommercial.Visibility = Visibility.Collapsed;
            }

            LoadAllAppointmentsByTab();
        }

        // ─────────────────────────────────────────────────
        //  CHARGEMENT COMMERCIAUX (Manager uniquement)
        // ─────────────────────────────────────────────────
        private void LoadCommerciaux()
        {
            var commerciaux = new List<Commercial>();
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    // Charge tous les utilisateurs (Manager + Commercial)
                    // Adaptez le filtre WHERE si vous ne voulez que les Commerciaux
                    string sql = "SELECT id, name FROM users ORDER BY name";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            commerciaux.Add(new Commercial
                            {
                                Id = reader.GetInt64("id"),
                                Name = reader["name"] != DBNull.Value ? reader["name"].ToString() : "(sans nom)"
                            });
                        }
                    }
                }
                cbCommerciaux.ItemsSource = commerciaux;
                cbCommerciaux.DisplayMemberPath = "Name";

                // Pré-sélectionner le Manager connecté par défaut
                var self = commerciaux.FirstOrDefault(c => c.Id == Session.UserId);
                if (self != null) cbCommerciaux.SelectedItem = self;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement commerciaux : " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────
        //  CHARGEMENT CLIENTS
        // ─────────────────────────────────────────────────
        private void LoadClients()
        {
            var clients = new List<Client>();
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = "SELECT * FROM customers WHERE status IN ('actif', 'prospect') ORDER BY name";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clients.Add(new Client()
                            {
                                Id = reader.GetInt64("id"),
                                Name = reader["name"] != DBNull.Value ? reader["name"].ToString() : "",
                                Email = reader["email"] != DBNull.Value ? reader["email"].ToString() : "",
                                Phone = reader["phone"] != DBNull.Value ? reader["phone"].ToString() : "",
                                Status = reader["status"] != DBNull.Value ? reader["status"].ToString() : ""
                            });
                        }
                    }
                }
                cbClients.ItemsSource = clients;
                cbClients.DisplayMemberPath = "Name";
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement clients : " + ex.Message); }
        }

        // ─────────────────────────────────────────────────
        //  CHARGEMENT RDV (par client)
        // ─────────────────────────────────────────────────
        private void LoadAppointments(long clientId)
        {
            _allAppointments = new List<Appointment>();
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = @"SELECT a.id, a.customer_id, a.user_id,
                                          c.name  AS ClientName,
                                          c.status AS ClientStatus,
                                          a.start_at, a.subject,
                                          u.name  AS CommercialName
                                   FROM appointments a
                                   JOIN customers c ON a.customer_id = c.id
                                   LEFT JOIN users u ON a.user_id = u.id
                                   WHERE a.customer_id = @clientId
                                   ORDER BY a.start_at DESC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@clientId", clientId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allAppointments.Add(new Appointment()
                            {
                                Id = reader.GetInt64("id"),
                                ClientId = reader.GetInt64("customer_id"),
                                CommercialId = reader["user_id"] != DBNull.Value ? reader.GetInt64("user_id") : (long?)null,
                                ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString() : "",
                                ClientStatus = reader["ClientStatus"] != DBNull.Value ? reader["ClientStatus"].ToString() : "",
                                CommercialName = reader["CommercialName"] != DBNull.Value ? reader["CommercialName"].ToString() : "—",
                                StartAt = Convert.ToDateTime(reader["start_at"]),
                                Subject = reader["subject"] != DBNull.Value ? reader["subject"].ToString() : ""
                            });
                        }
                    }
                }
                ApplyTabFilter();
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement RDV : " + ex.Message); }
        }

        // ─────────────────────────────────────────────────
        //  CHARGEMENT RDV (par onglet actif/prospect)
        // ─────────────────────────────────────────────────
        private void LoadAllAppointmentsByTab()
        {
            _allAppointments = new List<Appointment>();
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = @"SELECT a.id, a.customer_id, a.user_id,
                                          c.name  AS ClientName,
                                          c.status AS ClientStatus,
                                          a.start_at, a.subject,
                                          u.name  AS CommercialName
                                   FROM appointments a
                                   JOIN customers c ON a.customer_id = c.id
                                   LEFT JOIN users u ON a.user_id = u.id
                                   WHERE c.status = @status
                                   ORDER BY a.start_at DESC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@status", _currentTab);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allAppointments.Add(new Appointment()
                            {
                                Id = reader.GetInt64("id"),
                                ClientId = reader.GetInt64("customer_id"),
                                CommercialId = reader["user_id"] != DBNull.Value ? reader.GetInt64("user_id") : (long?)null,
                                ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString() : "",
                                ClientStatus = reader["ClientStatus"] != DBNull.Value ? reader["ClientStatus"].ToString() : "",
                                CommercialName = reader["CommercialName"] != DBNull.Value ? reader["CommercialName"].ToString() : "—",
                                StartAt = Convert.ToDateTime(reader["start_at"]),
                                Subject = reader["subject"] != DBNull.Value ? reader["subject"].ToString() : ""
                            });
                        }
                    }
                }
                dgAppointments.ItemsSource = _allAppointments;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement RDV : " + ex.Message); }
        }

        private void ApplyTabFilter()
        {
            dgAppointments.ItemsSource = _allAppointments
                .Where(a => a.ClientStatus == _currentTab).ToList();
        }

        // ─────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────

        /// <summary>
        /// Retourne l'ID du commercial à écrire en BDD.
        /// Manager → ComboBox cbCommerciaux ; Commercial → Session.UserId
        /// </summary>
        private long? GetTargetUserId()
        {
            if (Session.IsManager)
            {
                return (cbCommerciaux.SelectedItem as Commercial)?.Id;
            }
            return Session.UserId;
        }

        // ─────────────────────────────────────────────────
        //  ÉVÉNEMENTS UI
        // ─────────────────────────────────────────────────

        private void btnTabClients_Click(object sender, RoutedEventArgs e)
        {
            _currentTab = "actif";
            btnTabClients.Style = (Style)FindResource("TabButtonActive");
            btnTabProspects.Style = (Style)FindResource("TabButtonInactive");
            LoadAllAppointmentsByTab();
        }

        private void btnTabProspects_Click(object sender, RoutedEventArgs e)
        {
            _currentTab = "prospect";
            btnTabProspects.Style = (Style)FindResource("TabButtonActive");
            btnTabClients.Style = (Style)FindResource("TabButtonInactive");
            LoadAllAppointmentsByTab();
        }

        private void cbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbClients.SelectedItem is Client selectedClient)
                LoadAppointments(selectedClient.Id);
        }

        // Événement déclaratif dans le XAML (ne fait rien de spécial, juste pour éviter l'erreur de compilation)
        private void cbCommerciaux_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ─────────────────────────────────────────────────
        //  AJOUTER UN RDV
        // ─────────────────────────────────────────────────
        private void btnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (!(cbClients.SelectedItem is Client selectedClient))
            {
                MessageBox.Show("Veuillez sélectionner un client ou prospect.");
                return;
            }

            DateTime? date = dpAppointmentDate.SelectedDate;
            string subject = txtAppointmentSubject.Text;

            if (date == null || string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show("Veuillez remplir la date et le sujet du rendez-vous.");
                return;
            }

            long? targetUserId = GetTargetUserId();

            // Si Manager et aucun commercial sélectionné
            if (Session.IsManager && targetUserId == null)
            {
                MessageBox.Show("Veuillez sélectionner un commercial à assigner.");
                return;
            }

            DateTime dateTime = date.Value.Date.Add(DateTime.Now.TimeOfDay);
            Database db = new Database();
            try
            {
                long newId;
                using (var conn = db.GetConnection())
                {
                    string sql = @"INSERT INTO appointments (customer_id, user_id, start_at, subject, created_at)
                                   VALUES (@clientId, @userId, @startAt, @subject, NOW());
                                   SELECT LAST_INSERT_ID();";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@clientId", selectedClient.Id);
                    cmd.Parameters.AddWithValue("@userId", (object)targetUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@startAt", dateTime);
                    cmd.Parameters.AddWithValue("@subject", subject);
                    newId = Convert.ToInt64(cmd.ExecuteScalar());
                }

                // Récupérer le nom du commercial pour l'audit
                string commercialName = Session.IsManager
                    ? (cbCommerciaux.SelectedItem as Commercial)?.Name ?? "—"
                    : Session.CurrentUser;

                var log = new
                {
                    avant = (object)null,
                    apres = new
                    {
                        client = selectedClient.Name,
                        statut_client = selectedClient.Status,
                        date = dateTime.ToString("dd/MM/yyyy HH:mm"),
                        sujet = subject,
                        commercial = commercialName
                    }
                };
                AuditService.AddLog("INSERT", "appointments", newId, JsonSerializer.Serialize(log));

                MessageBox.Show("Rendez-vous ajouté avec succès !");
                LoadAppointments(selectedClient.Id);
            }
            catch (Exception ex) { MessageBox.Show("Erreur ajout RDV : " + ex.Message); }
        }

        // ─────────────────────────────────────────────────
        //  SÉLECTION DANS LA GRILLE
        // ─────────────────────────────────────────────────
        private void dgAppointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAppointments.SelectedItem is Appointment selectedAppointment)
            {
                dpAppointmentDate.SelectedDate = selectedAppointment.StartAt;
                txtAppointmentSubject.Text = selectedAppointment.Subject;

                if (cbClients.ItemsSource is List<Client> clients)
                {
                    var match = clients.FirstOrDefault(c => c.Id == selectedAppointment.ClientId);
                    if (match != null) cbClients.SelectedItem = match;
                }

                // Resélectionner le commercial dans le ComboBox (Manager uniquement)
                if (Session.IsManager && selectedAppointment.CommercialId.HasValue
                    && cbCommerciaux.ItemsSource is List<Commercial> commerciaux)
                {
                    var matchC = commerciaux.FirstOrDefault(c => c.Id == selectedAppointment.CommercialId.Value);
                    if (matchC != null) cbCommerciaux.SelectedItem = matchC;
                }
            }
        }

        // ─────────────────────────────────────────────────
        //  MODIFIER UN RDV
        // ─────────────────────────────────────────────────
        private void btnUpdateAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgAppointments.SelectedItem is Appointment selected))
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous à modifier.");
                return;
            }

            DateTime? date = dpAppointmentDate.SelectedDate;
            string subject = txtAppointmentSubject.Text;

            if (date == null || string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show("Veuillez remplir la date et le sujet.");
                return;
            }

            long? targetUserId = GetTargetUserId();

            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = @"UPDATE appointments
                                   SET start_at=@startAt, subject=@subject,
                                       user_id=@userId, updated_at=NOW()
                                   WHERE id=@id";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@startAt", date.Value);
                    cmd.Parameters.AddWithValue("@subject", subject);
                    cmd.Parameters.AddWithValue("@userId", (object)targetUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                string commercialName = Session.IsManager
                    ? (cbCommerciaux.SelectedItem as Commercial)?.Name ?? "—"
                    : Session.CurrentUser;

                var log = new
                {
                    avant = new { client = selected.ClientName, date = selected.StartAt.ToString("dd/MM/yyyy HH:mm"), sujet = selected.Subject, commercial = selected.CommercialName },
                    apres = new { client = selected.ClientName, date = date.Value.ToString("dd/MM/yyyy HH:mm"), sujet = subject, commercial = commercialName }
                };
                AuditService.AddLog("UPDATE", "appointments", selected.Id, JsonSerializer.Serialize(log));

                MessageBox.Show("Rendez-vous mis à jour !");
                LoadAllAppointmentsByTab();
            }
            catch (Exception ex) { MessageBox.Show("Erreur update RDV : " + ex.Message); }
        }

        // ─────────────────────────────────────────────────
        //  SUPPRIMER UN RDV
        // ─────────────────────────────────────────────────
        private void btnDeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgAppointments.SelectedItem is Appointment selected))
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous à supprimer.");
                return;
            }

            if (MessageBox.Show("Supprimer ce rendez-vous ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Database db = new Database();
                try
                {
                    using (var conn = db.GetConnection())
                    {
                        MySqlCommand cmd = new MySqlCommand("DELETE FROM appointments WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", selected.Id);
                        cmd.ExecuteNonQuery();
                    }

                    var log = new
                    {
                        avant = new { client = selected.ClientName, statut_client = selected.ClientStatus, date = selected.StartAt.ToString("dd/MM/yyyy HH:mm"), sujet = selected.Subject, commercial = selected.CommercialName },
                        apres = (object)null
                    };
                    AuditService.AddLog("DELETE", "appointments", selected.Id, JsonSerializer.Serialize(log));

                    MessageBox.Show("Rendez-vous supprimé !");
                    LoadAllAppointmentsByTab();
                }
                catch (Exception ex) { MessageBox.Show("Erreur suppression RDV : " + ex.Message); }
            }
        }
    }
}