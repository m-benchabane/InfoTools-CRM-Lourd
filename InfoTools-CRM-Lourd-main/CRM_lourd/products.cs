namespace CRM_lourd
{
    public class Product
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } // NEW
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
