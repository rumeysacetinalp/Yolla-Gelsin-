using System.ComponentModel.DataAnnotations;

namespace StokYönetimi.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        public string? ProductName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stok 0 veya daha büyük olmalıdır.")]
        public int? Stock { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır.")]
        public decimal? Price { get; set; }

        public string? ProductImage { get; set; }

        public string? Category { get; set; }
    }

}
