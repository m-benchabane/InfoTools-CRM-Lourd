using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CRM_lourd.Views
{
    public partial class ProductsView : UserControl
    {
        public ProductsView()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void LoadProducts()
        {
            List<Product> products = new List<Product>();
            Database db = new Database();
            try
            {
                var conn = db.GetConnection();
                MySqlDataReader reader = new MySqlCommand("SELECT * FROM products", conn).ExecuteReader();
                while (reader.Read())
                {
                    products.Add(new Product()
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader["name"] as string,
                        Description = reader["description"] as string,
                        Stock = reader.GetInt32("stock"),
                        Price = reader.GetDecimal("price")
                    });
                }
                reader.Close();
                dgProducts.ItemsSource = products;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            string name = txtProductName.Text;
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Le nom du produit est obligatoire."); return; }

            if (!int.TryParse(txtProductStock.Text, out int stock)) stock = 0;
            if (!decimal.TryParse(txtProductPrice.Text, out decimal price)) price = 0;

            Database db = new Database();
            try
            {
                var conn = db.GetConnection();
                MySqlCommand cmd = new MySqlCommand(
                    "INSERT INTO products (name, description, stock, price, created_at) VALUES (@name, '', @stock, @price, NOW())", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@stock", stock);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Produit ajouté !");
                LoadProducts();
            }
            catch (Exception ex) { MessageBox.Show("Erreur SQL : " + ex.Message); }
        }

        private void btnUpdateProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgProducts.SelectedItem is Product selected)) return;

            if (!int.TryParse(txtProductStock.Text, out int stock)) stock = selected.Stock;
            if (!decimal.TryParse(txtProductPrice.Text, out decimal price)) price = selected.Price;

            Database db = new Database();
            try
            {
                var conn = db.GetConnection();
                MySqlCommand cmd = new MySqlCommand(
                    "UPDATE products SET name=@name, stock=@stock, price=@price, updated_at=NOW() WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@name", txtProductName.Text);
                cmd.Parameters.AddWithValue("@stock", stock);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@id", selected.Id);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Produit mis à jour !");
                LoadProducts();
            }
            catch (Exception ex) { MessageBox.Show("Erreur SQL : " + ex.Message); }
        }

        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgProducts.SelectedItem is Product selected)) return;

            if (MessageBox.Show("Supprimer ?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Database db = new Database();
                try
                {
                    var conn = db.GetConnection();
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM products WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Produit supprimé !");
                    LoadProducts();
                }
                catch (Exception ex) { MessageBox.Show("Erreur SQL : " + ex.Message); }
            }
        }
    }
}