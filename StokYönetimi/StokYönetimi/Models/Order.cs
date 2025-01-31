namespace StokYönetimi.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public Customer Customer { get; set; } // Navigation property
        public int ProductID { get; set; }
        public Product Product { get; set; } // Navigation property
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } // Örneğin: Tamamlandı, İptal
    }

}
