using System.ComponentModel.DataAnnotations;

namespace StokYönetimi.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad 100 karakterden fazla olamaz.")]
        public string CustomerName { get; set; }

        public decimal? Budget { get; set; }

        [StringLength(50, ErrorMessage = "Müşteri türü 50 karakterden fazla olamaz.")]
        public string CustomerType { get; set; }

        public decimal? TotalSpent { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kullanıcı adı 100 karakterden fazla olamaz.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Soyad 100 karakterden fazla olamaz.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; }

        [StringLength(10, ErrorMessage = "Cinsiyet 10 karakterden fazla olamaz.")]
        public string Gender { get; set; }
    }
}