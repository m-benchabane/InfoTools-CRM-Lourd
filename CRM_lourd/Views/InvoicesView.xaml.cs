using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace CRM_lourd.Views
{
    public partial class InvoicesView : UserControl
    {
        private ObservableCollection<Invoice_lines> _basket = new ObservableCollection<Invoice_lines>();
        private long? _selectedInvoiceId = null;
        private decimal _oldTotal = 0;
        private string _oldClientName = "";

        public InvoicesView()
        {
            InitializeComponent();
            lbBasket.ItemsSource = _basket;
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            try { LoadClients(); LoadProducts(); }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void LoadClients()
        {
            var list = new List<Client>();
            Database db = new Database();
            using (var conn = db.GetConnection())
            {
                var r = new MySqlCommand("SELECT id, name FROM customers WHERE status = 'actif'", conn).ExecuteReader();
                while (r.Read()) list.Add(new Client { Id = r.GetInt64(0), Name = r.GetString(1) });
            }
            cbClients.ItemsSource = list;
        }

        private void LoadProducts()
        {
            var list = new List<Product>();
            Database db = new Database();
            using (var conn = db.GetConnection())
            {
                var r = new MySqlCommand("SELECT id, name, price FROM products", conn).ExecuteReader();
                while (r.Read()) list.Add(new Product { Id = r.GetInt64(0), Name = r.GetString(1), Price = r.GetDecimal(2) });
            }
            cbProducts.ItemsSource = list;
        }

        private void LoadInvoices(long clientId)
        {
            var list = new List<Invoice>();
            Database db = new Database();
            using (var conn = db.GetConnection())
            {
                var cmd = new MySqlCommand("SELECT id, invoiced_at, total FROM invoices WHERE customer_id=@id ORDER BY id DESC", conn);
                cmd.Parameters.AddWithValue("@id", clientId);
                var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(new Invoice { Id = r.GetInt64(0), InvoicedAt = r.GetDateTime(1), Total = r.GetDecimal(2), CustomerId = clientId });
            }
            dgInvoices.ItemsSource = list;
        }

        private void btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            if (cbProducts.SelectedItem is Product p && int.TryParse(txtQty.Text, out int qty))
            {
                _basket.Add(new Invoice_lines
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Qty = qty,
                    UnitPrice = p.Price,
                    LineTotal = qty * p.Price
                });
                UpdateTotal();
            }
        }

        private void UpdateTotal()
        {
            txtInvoiceTotal.Text = _basket.Sum(x => x.LineTotal).ToString("N2") + " €";
        }

        private void dgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInvoices.SelectedItem is Invoice inv)
            {
                _selectedInvoiceId = inv.Id;
                _oldTotal = inv.Total;
                _oldClientName = (cbClients.SelectedItem as Client)?.Name ?? "";
                dpInvoiceDate.SelectedDate = inv.InvoicedAt;

                _basket.Clear();
                var items = new List<Invoice_lines>();
                Database db = new Database();
                using (var conn = db.GetConnection())
                {
                    var cmd = new MySqlCommand("SELECT il.*, p.name FROM invoice_lines il JOIN products p ON il.product_id = p.id WHERE invoice_id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", inv.Id);
                    var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        var line = new Invoice_lines
                        {
                            ProductId = r.GetInt64("product_id"),
                            ProductName = r.GetString("name"),
                            Qty = r.GetInt32("qty"),
                            UnitPrice = r.GetDecimal("unit_price"),
                            LineTotal = r.GetDecimal("line_total")
                        };
                        _basket.Add(line);
                        items.Add(line);
                    }
                }
                dgInvoiceItems.ItemsSource = items;
                UpdateTotal();
            }
        }

        private void btnAddInvoice_Click(object sender, RoutedEventArgs e) => SaveInvoice(null);
        private void btnUpdateInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedInvoiceId == null) return;
            SaveInvoice(_selectedInvoiceId);
        }

        private void SaveInvoice(long? existingId)
        {
            if (!(cbClients.SelectedItem is Client c) || _basket.Count == 0) return;

            Database db = new Database();
            using (var conn = db.GetConnection())
            {
                decimal newTotal = _basket.Sum(x => x.LineTotal);
                long id;

                if (existingId == null)
                {
                    var cmd = new MySqlCommand("INSERT INTO invoices (customer_id, invoiced_at, total, reference) VALUES (@cid, @date, @total, @ref); SELECT LAST_INSERT_ID();", conn);
                    cmd.Parameters.AddWithValue("@cid", c.Id);
                    cmd.Parameters.AddWithValue("@date", dpInvoiceDate.SelectedDate ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@total", newTotal);
                    cmd.Parameters.AddWithValue("@ref", "INV-" + DateTime.Now.Ticks.ToString().Substring(10));
                    id = Convert.ToInt64(cmd.ExecuteScalar());

                    var log = new
                    {
                        avant = (object)null,
                        apres = new
                        {
                            client = c.Name,
                            date = (dpInvoiceDate.SelectedDate ?? DateTime.Now).ToString("dd/MM/yyyy"),
                            total = newTotal,
                            nb_lignes = _basket.Count
                        }
                    };
                    AuditService.AddLog("INSERT", "invoices", id, JsonSerializer.Serialize(log));
                }
                else
                {
                    id = existingId.Value;
                    var cmd = new MySqlCommand("UPDATE invoices SET customer_id=@cid, invoiced_at=@date, total=@total WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@cid", c.Id);
                    cmd.Parameters.AddWithValue("@date", dpInvoiceDate.SelectedDate);
                    cmd.Parameters.AddWithValue("@total", newTotal);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    new MySqlCommand($"DELETE FROM invoice_lines WHERE invoice_id={id}", conn).ExecuteNonQuery();

                    var log = new
                    {
                        avant = new { client = _oldClientName, total = _oldTotal },
                        apres = new { client = c.Name, date = dpInvoiceDate.SelectedDate?.ToString("dd/MM/yyyy"), total = newTotal, nb_lignes = _basket.Count }
                    };
                    AuditService.AddLog("UPDATE", "invoices", id, JsonSerializer.Serialize(log));
                }

                foreach (var l in _basket)
                {
                    var cmdL = new MySqlCommand("INSERT INTO invoice_lines (invoice_id, product_id, qty, unit_price, line_total) VALUES (@iid, @pid, @q, @up, @lt)", conn);
                    cmdL.Parameters.AddWithValue("@iid", id);
                    cmdL.Parameters.AddWithValue("@pid", l.ProductId);
                    cmdL.Parameters.AddWithValue("@q", l.Qty);
                    cmdL.Parameters.AddWithValue("@up", l.UnitPrice);
                    cmdL.Parameters.AddWithValue("@lt", l.LineTotal);
                    cmdL.ExecuteNonQuery();
                }

                MessageBox.Show("Opération réussie !");
                LoadInvoices(c.Id);
            }
        }

        private void btnDeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedInvoiceId == null) return;
            if (MessageBox.Show("Supprimer ?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var client = cbClients.SelectedItem as Client;
                Database db = new Database();
                using (var conn = db.GetConnection())
                {
                    new MySqlCommand($"DELETE FROM invoice_lines WHERE invoice_id={_selectedInvoiceId}", conn).ExecuteNonQuery();
                    new MySqlCommand($"DELETE FROM invoices WHERE id={_selectedInvoiceId}", conn).ExecuteNonQuery();

                    var log = new
                    {
                        avant = new { client = client?.Name, total = _oldTotal },
                        apres = (object)null
                    };
                    AuditService.AddLog("DELETE", "invoices", _selectedInvoiceId, JsonSerializer.Serialize(log));

                    if (client != null) LoadInvoices(client.Id);
                    btnReset_Click(null, null);
                }
            }
        }

        private void cbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbClients.SelectedItem is Client c) LoadInvoices(c.Id);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            _selectedInvoiceId = null;
            _oldTotal = 0;
            _oldClientName = "";
            _basket.Clear();
            UpdateTotal();
            dpInvoiceDate.SelectedDate = DateTime.Now;
        }
    }
}