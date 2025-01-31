using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokYönetimi.Models
{
    public class ShoppingCart
    {
        [Key]
        public int CartID { get; set; } // Sepet ID

        [Required(ErrorMessage = "Müşteri ID'si zorunludur.")]
        public int CustomerID { get; set; } 

        [Required(ErrorMessage = "Ürün ID'si zorunludur.")]
        public int ProductID { get; set; } // Ürün ID

        [Required(ErrorMessage = "Miktar zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar en az 1 olmalıdır.")]
        public int Quantity { get; set; } // Ürün miktarı

        public DateTime? DateAdded { get; set; } = DateTime.Now; 

        // Navigation Properties
        public Customer? Customer { get; set; } // Müşteri ile ilişki
        public Product? Product { get; set; }   // Ürün ile ilişki
    }
}