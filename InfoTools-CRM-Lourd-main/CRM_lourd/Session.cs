namespace CRM_lourd
{
    public static class Session
    {
        // AJOUT : Stocke l'ID numérique de l'utilisateur pour la BDD
        // On initialise à 1 par défaut pour le développement
        public static long UserId { get; set; } = 1;

        public static string CurrentUser { get; set; }
        public static string Role { get; set; }

        public static bool IsManager => Role == "Manager";
    }
}