using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CRM_lourd; // Pour accéder à la classe Client et Database

namespace CRM_lourd.Views
{
    public partial class AppointmentsView : UserControl
    {
        public AppointmentsView()
        {
            InitializeComponent();
            LoadClients();
        }

        // CHARGEMENT DES CLIENTS
        private void LoadClients()
        {
            List<Client> clients = new List<Client>();
            Database db = new Database();

            try
            {
                // Le bloc "using" ferme automatiquement la connexion à la fin
                using (var conn = db.GetConnection())
                {
                    // PAS DE conn.Open() ICI ! Elle est déjà ouverte par GetConnection()

                    string sql = "SELECT * FROM customers";
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
                                Phone = reader["phone"] != DBNull.Value ? reader["phone"].ToString() : ""
                            });
                        }
                    }
                }

                cbClients.ItemsSource = clients;
                cbClients.DisplayMemberPath = "Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement clients : " + ex.Message);
            }
        }

        // CHARGEMENT DES RENDEZ-VOUS D'UN CLIENT
        private void LoadAppointments(long clientId)
        {
            List<Appointment> appointments = new List<Appointment>();
            Database db = new Database();

            try
            {
                using (var conn = db.GetConnection())
                {
                    // PAS DE conn.Open() ICI !

                    string sql = @"SELECT a.id, a.customer_id, c.name AS ClientName, a.start_at, a.subject
                                   FROM appointments a 
                                   JOIN customers c ON a.customer_id = c.id 
                                   WHERE a.customer_id = @clientId ORDER BY a.start_at DESC";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@clientId", clientId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            appointments.Add(new Appointment()
                            {
                                Id = reader.GetInt64("id"),
                                ClientId = reader.GetInt64("customer_id"),
                                ClientName = reader["ClientName"] != DBNull.Value ? reader["ClientName"].ToString() : "",
                                StartAt = Convert.ToDateTime(reader["start_at"]),
                                Subject = reader["subject"] != DBNull.Value ? reader["subject"].ToString() : ""
                            });
                        }
                    }
                }

                dgAppointments.ItemsSource = appointments;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement RDV : " + ex.Message);
            }
        }

        private void cbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbClients.SelectedItem is Client selectedClient)
                LoadAppointments(selectedClient.Id);
        }

        // AJOUTER UN RENDEZ-VOUS
        private void btnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (cbClients.SelectedItem is Client selectedClient)
            {
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
                    using (var conn = db.GetConnection())
                    {
                        // PAS DE conn.Open() ICI !

                        string sql = @"INSERT INTO appointments (customer_id, start_at, subject, created_at) 
                                       VALUES (@clientId, @startAt, @subject, NOW())";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@clientId", selectedClient.Id);
                        cmd.Parameters.AddWithValue("@startAt", dateTime);
                        cmd.Parameters.AddWithValue("@subject", subject);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Rendez-vous ajouté avec succès !");
                    LoadAppointments(selectedClient.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur ajout RDV : " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client.");
            }
        }

        private void dgAppointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAppointments.SelectedItem is Appointment selectedAppointment)
            {
                dpAppointmentDate.SelectedDate = selectedAppointment.StartAt;
                txtAppointmentSubject.Text = selectedAppointment.Subject;
            }
        }

        // MODIFIER UN RENDEZ-VOUS
        private void btnUpdateAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dgAppointments.SelectedItem is Appointment selectedAppointment)
            {
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
                        // PAS DE conn.Open() ICI !

                        string sql = @"UPDATE appointments 
                                       SET start_at=@startAt, subject=@subject, updated_at=NOW() 
                                       WHERE id=@id";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@startAt", date.Value);
                        cmd.Parameters.AddWithValue("@subject", subject);
                        cmd.Parameters.AddWithValue("@id", selectedAppointment.Id);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Rendez-vous mis à jour !");
                    LoadAppointments(selectedAppointment.ClientId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur update RDV : " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous à modifier.");
            }
        }

        // SUPPRIMER UN RENDEZ-VOUS
        private void btnDeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dgAppointments.SelectedItem is Appointment selectedAppointment)
            {
                if (MessageBox.Show("Supprimer ce rendez-vous ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Database db = new Database();
                    try
                    {
                        using (var conn = db.GetConnection())
                        {
                            // PAS DE conn.Open() ICI !

                            string sql = "DELETE FROM appointments WHERE id=@id";
                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@id", selectedAppointment.Id);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Rendez-vous supprimé !");
                        LoadAppointments(selectedAppointment.ClientId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erreur suppression RDV : " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous à supprimer.");
            }
        }
    }
}