using MySql.Data.MySqlClient;
using System;
using System.Text.Json;

namespace CRM_lourd.Views
{
    public static class AuditService
    {
        public static void AddLog(string action, string tableName, long? rowId, string changes)
        {
            Database db = new Database();
            try
            {
                using (var conn = db.GetConnection())
                {
                    long userId = Session.UserId > 0 ? Session.UserId : 1;
                    string sql = "INSERT INTO audit_logs (user_id, table_name, row_id, action, changed, created_at) " +
                                 "VALUES (@uid, @table, @rid, @act, @chg, NOW())";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);

                    // On envoie directement la string JSON sans la re-sérialiser
                    string jsonChanges = changes ?? "{\"info\":\"Aucun détail\"}";

                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@table", tableName);
                    cmd.Parameters.AddWithValue("@rid", rowId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@act", action);
                    cmd.Parameters.AddWithValue("@chg", jsonChanges);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erreur audit : " + ex.Message);
            }
        }
    }
}