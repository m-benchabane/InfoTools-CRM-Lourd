using System;

namespace CRM_lourd
{
    public class Appointment
    {
        public long Id { get; set; }
        public long ClientId { get; set; }
        public string ClientName { get; set; }
        public DateTime StartAt { get; set; }
        public string Subject { get; set; }
    }
}
