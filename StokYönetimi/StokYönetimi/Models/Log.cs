namespace StokYönetimi.Models
{
    public class Log
    {
        public int LogID { get; set; }
        public int? CustomerID { get; set; } // Nullable yapılabilir
        public Customer? Customer { get; set; } 
        public int? OrderID { get; set; }
        public Order? Order { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now; // Varsayılan tarih
        public string LogType { get; set; } = string.Empty; // Boş bırakılmaması için
        public string LogDetails { get; set; } = string.Empty; // Varsayılan değer
    }


}
