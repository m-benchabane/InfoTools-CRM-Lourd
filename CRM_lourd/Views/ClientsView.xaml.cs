using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CRM_lourd;

namespace CRM_lourd.Views
{
    public partial class ClientsView : UserControl
    {
        public ClientsView()
        {
            InitializeComponent();
            LoadClients();
        }

        private void LoadClients()
        {
            List<Client> clients = new List<Client>();
            Database db = new Database();
            try
            {
                var conn = db.GetConnection();
                MySqlDataReader reader = new MySqlCommand("SELECT * FROM customers ORDER BY created_at DESC", conn).ExecuteReader();
                while (reader.Read())
                {
                    clients.Add(new Client()
                    {
                        Id = reader.GetInt64("id"),
                        Name = reader["name"] != DBNull.Value ? reader["name"].ToString() : "",
                        CompanyName = reader["company_name"] != DBNull.Value ? reader["company_name"].ToString() : "",
                        Email = reader["email"] != DBNull.Value ? reader["email"].ToString() : "",
                        Phone = reader["phone"] != DBNull.Value ? reader["phone"].ToString() : "",
                        Address = reader["address"] != DBNull.Value ? reader["address"].ToString() : "",
                        City = reader["city"] != DBNull.Value ? reader["city"].ToString() : "",
                        PostalCode = reader["postal_code"] != DBNull.Value ? reader["postal_code"].ToString() : "",
                        Status = reader["status"] != DBNull.Value ? reader["status"].ToString() : "prospect"
                    });
                }
                reader.Close();
                dgCustomers.ItemsSource = clients;
            }
            catch (Exception ex) { MessageBox.Show("Erreur de chargement : " + ex.Message); }
        }

        private void dgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCustomers.SelectedItem is Client selected)
            {
                txtCustomerName.Text = selected.Name;
                txtCompany.Text = selected.CompanyName;
                txtCustomerEmail.Text = selected.Email;
                txtCustomerPhone.Text = selected.Phone;
                txtAddress.Text = selected.Address;
                txtCity.Text = selected.City;
                txtZip.Text = selected.PostalCode;

                if (cbStatus != null)
                {
                    foreach (ComboBoxItem item in cbStatus.Items)
                    {
                        if (item.Content != null && item.Content.ToString().ToLower() == selected.Status?.ToLower())
                        {
                            cbStatus.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void btnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            string name = txtCustomerName.Text;
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Le nom est obligatoire."); return; }

            string status = "prospect";
            if (cbStatus.SelectedItem is ComboBoxItem si && si.Content != null)
                status = si.Content.ToString();

            Database db = new Database();
            try
            {
                var conn = db.GetConnection();
                string sql = @"INSERT INTO customers 
                               (name, company_name, email, phone, address, city, postal_code, status, created_at) 
                               VALUES (@name, @company, @email, @phone, @address, @city, @zip, @status, NOW())";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@company", txtCompany.Text);
                cmd.Parameters.AddWithValue("@email", txtCustomerEmail.Text);
                cmd.Parameters.AddWithValue("@phone", txtCustomerPhone.Text);
                cmd.Parameters.AddWithValue("@address", txtAddress.Text);
                cmd.Parameters.AddWithValue("@city", txtCity.Text);
                cmd.Parameters.AddWithValue("@zip", txtZip.Text);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.ExecuteNonQuery();

                long newId = cmd.LastInsertedId;
                AuditService.AddLog("INSERT", "customers", newId, $"Ajout contact : {name} ({status})");

                MessageBox.Show("Contact ajouté !");
                LoadClients();
                ClearForm();
            }
            catch (Exception ex) { MessageBox.Show("Erreur SQL : " + ex.Message); }
        }

        private void btnUpdateCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgCustomers.SelectedItem is Client selected))
            {
                MessageBox.Show("Veuillez sélectionner un contact.");
                return;
            }

            string status = "prospect";
            if (cbStatus.SelectedItem is ComboBoxItem si && si.Content != null)
                status = si.Content.ToString();

            Database db = new Database();
            try
            {
                var conn = db.GetConnection();
                string sql = @"UPDATE customers SET 
                               name=@name, company_name=@company, email=@email, phone=@phone, 
                               address=@address, city=@city, postal_code=@zip, status=@status, 
                               updated_at=NOW() WHERE id=@id";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", txtCustomerName.Text);
                cmd.Parameters.AddWithValue("@company", txtCompany.Text);
                cmd.Parameters.AddWithValue("@email", txtCustomerEmail.Text);
                cmd.Parameters.AddWithValue("@phone", txtCustomerPhone.Text);
                cmd.Parameters.AddWithValue("@address", txtAddress.Text);
                cmd.Parameters.AddWithValue("@city", txtCity.Text);
                cmd.Parameters.AddWithValue("@zip", txtZip.Text);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", selected.Id);
                cmd.ExecuteNonQuery();

                AuditService.AddLog("UPDATE", "customers", selected.Id,
                    $"Modification contact ID {selected.Id} : {selected.Name} -> {txtCustomerName.Text} ({status})");

                MessageBox.Show("Contact mis à jour !");
                LoadClients();
                ClearForm();
            }
            catch (Exception ex) { MessageBox.Show("Erreur SQL : " + ex.Message); }
        }

        private void btnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgCustomers.SelectedItem is Client selected)) return;

            if (MessageBox.Show("Supprimer ce contact ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Database db = new Database();
                try
                {
                    var conn = db.GetConnection();
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM customers WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();

                    AuditService.AddLog("DELETE", "customers", selected.Id,
                        $"Suppression contact : {selected.Name} ({selected.Status})");

                    MessageBox.Show("Contact supprimé.");
                    LoadClients();
                    ClearForm();
                }
                catch (Exception ex) { MessageBox.Show("Erreur SQL : " + ex.Message); }
            }
        }

        private void ClearForm()
        {
            txtCustomerName.Text = "";
            txtCompany.Text = "";
            txtCustomerEmail.Text = "";
            txtCustomerPhone.Text = "";
            txtAddress.Text = "";
            txtCity.Text = "";
            txtZip.Text = "";
            if (cbStatus != null && cbStatus.Items.Count > 0) cbStatus.SelectedIndex = 0;
        }
    }
}