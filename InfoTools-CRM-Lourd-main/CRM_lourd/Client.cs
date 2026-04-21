namespace CRM_lourd
{
    public class Client
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Status { get; set; }
        public DateTime? LastContactAt { get; set; }
        public DateTime? NextMeetingAt { get; set; }
        public decimal TotalSpent { get; set; }
        public long? UserId { get; set; }

        // --- PROPRIÉTÉS POUR L'AFFICHAGE (UI) ---

        // Utilisé pour le cercle bleu dans ton Dashboard
        public string Initial
            => !string.IsNullOrEmpty(Name) ? Name.Substring(0, 1).ToUpper() : "?";

        public string DisplayName
            => $"{FirstName} {LastName} – {Email}";
    }
}