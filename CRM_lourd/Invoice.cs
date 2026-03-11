namespace CRM_lourd
{
    public class Invoice
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }

        public string Reference { get; set; }    // NEW
        public DateTime InvoicedAt { get; set; } // NEW

        public decimal Total { get; set; }

        public List<Invoice_lines> Items { get; set; } = new List<Invoice_lines>();

    }
}
