using MySql.Data.MySqlClient;
using System.Data;

namespace CRM_lourd
{
    internal class Database
    {
        private string connStr = "server=localhost;user=root;password=root;database=crm_laravel_like;port=3306;charset=utf8mb4";

        public Database()
        {
            // On ne fait plus connection.Open() ici !
            // On prépare juste la chaîne de connexion.
        }

        public MySqlConnection GetConnection()
        {
            // On retourne une NOUVELLE connexion à chaque appel.
            // C'est l'appelant (vos Vues) qui devra l'ouvrir.
            MySqlConnection connection = new MySqlConnection(connStr);

            // On l'ouvre immédiatement pour que vos vues existantes continuent de fonctionner
            // sans avoir à rajouter connection.Open() partout dans votre code actuel.
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }
    }
}