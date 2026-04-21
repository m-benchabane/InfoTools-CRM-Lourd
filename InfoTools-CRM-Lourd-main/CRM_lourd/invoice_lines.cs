namespace CRM_lourd
{
    public class Invoice_lines
    {
        public long Id { get; set; }
        public long InvoiceId { get; set; }
        public long ProductId { get; set; }

        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string ProductName { get; set; } // jointure
    }
}
