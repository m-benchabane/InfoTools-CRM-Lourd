using System;

namespace CRM_lourd
{
    public class AuditLog
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string TableName { get; set; }
        public long? RowId { get; set; }
        public string Action { get; set; } 
        public string Changed { get; set; } 
        public string Ip { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}