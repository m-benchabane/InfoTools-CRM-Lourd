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
    public partial class AppointmentsView : UserControl
    {
        private string _currentTab = "actif";
        private List<Appointment> _allAppointments = new List<Appointment>();

        public AppointmentsView()
        {
            InitializeComponent();
            LoadClients();
        }

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

        private void LoadAppointments(long clientId)
        {
            _allAppointments = new List<Appointment>();
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = @"SELECT a.id, a.customer_id, c.name AS ClientName,
                                          c.status AS ClientStatus, a.start_at, a.subject
                                   FROM appointments a
                                   JOIN customers c ON a.customer_id = c.id
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
                                ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString() : "",
                                ClientStatus = reader["ClientStatus"] != DBNull.Value ? reader["ClientStatus"].ToString() : "",
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

        private void LoadAllAppointmentsByTab()
        {
            _allAppointments = new List<Appointment>();
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = @"SELECT a.id, a.customer_id, c.name AS ClientName,
                                          c.status AS ClientStatus, a.start_at, a.subject
                                   FROM appointments a
                                   JOIN customers c ON a.customer_id = c.id
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
                                ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString() : "",
                                ClientStatus = reader["ClientStatus"] != DBNull.Value ? reader["ClientStatus"].ToString() : "",
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

            DateTime dateTime = date.Value.Date.Add(DateTime.Now.TimeOfDay);
            Database db = new Database();
            try
            {
                long newId;
                using (var conn = db.GetConnection())
                {
                    string sql = @"INSERT INTO appointments (customer_id, start_at, subject, created_at)
                                   VALUES (@clientId, @startAt, @subject, NOW());
                                   SELECT LAST_INSERT_ID();";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@clientId", selectedClient.Id);
                    cmd.Parameters.AddWithValue("@startAt", dateTime);
                    cmd.Parameters.AddWithValue("@subject", subject);
                    newId = Convert.ToInt64(cmd.ExecuteScalar());
                }

                var log = new
                {
                    avant = (object)null,
                    apres = new { client = selectedClient.Name, statut_client = selectedClient.Status, date = dateTime.ToString("dd/MM/yyyy HH:mm"), sujet = subject }
                };
                AuditService.AddLog("INSERT", "appointments", newId, JsonSerializer.Serialize(log));

                MessageBox.Show("Rendez-vous ajouté avec succès !");
                LoadAppointments(selectedClient.Id);
            }
            catch (Exception ex) { MessageBox.Show("Erreur ajout RDV : " + ex.Message); }
        }

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
            }
        }

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

            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string sql = @"UPDATE appointments
                                   SET start_at=@startAt, subject=@subject, updated_at=NOW()
                                   WHERE id=@id";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@startAt", date.Value);
                    cmd.Parameters.AddWithValue("@subject", subject);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                var log = new
                {
                    avant = new { client = selected.ClientName, date = selected.StartAt.ToString("dd/MM/yyyy HH:mm"), sujet = selected.Subject },
                    apres = new { client = selected.ClientName, date = date.Value.ToString("dd/MM/yyyy HH:mm"), sujet = subject }
                };
                AuditService.AddLog("UPDATE", "appointments", selected.Id, JsonSerializer.Serialize(log));

                MessageBox.Show("Rendez-vous mis à jour !");
                LoadAllAppointmentsByTab();
            }
            catch (Exception ex) { MessageBox.Show("Erreur update RDV : " + ex.Message); }
        }

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
                        avant = new { client = selected.ClientName, statut_client = selected.ClientStatus, date = selected.StartAt.ToString("dd/MM/yyyy HH:mm"), sujet = selected.Subject },
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