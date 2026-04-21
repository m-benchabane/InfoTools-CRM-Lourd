using System;

namespace CRM_lourd
{
    public class Appointment
    {

        public long? CommercialId { get; set; }   // FK vers users.id (nullable)
        public string CommercialName { get; set; }   // Affiché dans la grille
        public long Id { get; set; }
        public long ClientId { get; set; }
        public string ClientName { get; set; }

        public string ClientStatus { get; set; }
        public DateTime StartAt { get; set; }
        public string Subject { get; set; }
    }
}
