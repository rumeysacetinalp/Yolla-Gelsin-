using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using StokYönetimi.Data; 
using StokYönetimi.Models; 
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Data.SqlClient;

namespace StokYönetimi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }




        // Kayıt Olma
        [HttpPost]
        public async Task<IActionResult> Register(Customer model)
        {
            try
            {
                // Şifre Hashleme
                model.Password = HashHelper.HashPassword(model.Password);

                // Kullanıcı Veritabanına Ekleme
                _context.Customers.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kayıt başarılı! Lütfen giriş yapınız.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Kayıt sırasında hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Login");
        }





        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            try
            {
                var user = await _context.Customers.FirstOrDefaultAsync(u => u.Email == Email);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return View();
                }

                var isPasswordValid = HashHelper.VerifyPassword(Password, user.Password);

                if (!isPasswordValid)
                {
                    TempData["ErrorMessage"] = "Şifre yanlış.";
                    _logger.LogWarning($"Giriş başarısız. Hash uyumsuzluğu. Kullanıcı: {Email}");
                    return View();
                }

                // Session'a kullanıcı ID ekleme
                HttpContext.Session.SetInt32("KullaniciID", user.CustomerID);
                _logger.LogInformation($"Session KullaniciID kaydedildi: {user.CustomerID}");


                _logger.LogInformation($"Giriş başarılı. Kullanıcı: {Email}");
                return RedirectToAction("Dashboard", "UserDashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Giriş sırasında hata oluştu: {ex.Message}";
                _logger.LogError($"Giriş sırasında hata: {ex.Message}");
                return View();
            }
        }


        [HttpPost]
        public async Task<IActionResult> AdminLogin(string Email, string Password)
        {
            try
            {
                // Admin tablosunda AdminNamee göre kayıt arama
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminName == Email);

                if (admin == null)
                {
                    TempData["ErrorMessage"] = "Admin bulunamadı.";
                    return RedirectToAction("Login");
                }

                // Şifre hash doğrulaması
                var isPasswordValid = HashHelper.VerifyPassword(Password, admin.Password);
                if (!isPasswordValid)
                {
                    TempData["ErrorMessage"] = "Şifre yanlış.";
                    return RedirectToAction("Login");
                }

              
             
                return RedirectToAction("admin", "Admin"); 
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Admin girişi sırasında hata oluştu: {ex.Message}";
                _logger.LogError($"Admin girişi sırasında hata: {ex.Message}");
                return RedirectToAction("Login");
            }
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }






        [HttpPost]
        public async Task<IActionResult> kayitOl(Customer model)
        {
            try
            {
               
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    TempData["ErrorMessage"] = "Şifre alanı boş bırakılamaz.";
                    return RedirectToAction("Login");
                }

                // Kullanıcının Şifresini Hashle
                model.Password = HashHelper.HashPassword(model.Password);

                // Bütçe alanı kontrolü
                if (model.Budget < 500 || model.Budget > 3000)
                {
                    TempData["ErrorMessage"] = "Bütçe 500 TL ile 3000 TL arasında olmalıdır.";
                    return RedirectToAction("Login");
                }

                // Eksik alanlara NULL değer atama veya varsayılan değerler
                model.Budget = model.Budget ?? 0; 
                model.TotalSpent = model.TotalSpent ?? 0; 
                model.CustomerType = model.CustomerType ?? "Standard"; 

                // Kullanıcıyı Veritabanına Ekle
                _context.Customers.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kayıt başarılı! Lütfen giriş yapınız.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Kayıt sırasında hata oluştu: {ex.Message}";
                _logger.LogError(ex, "Kayıt işlemi sırasında hata oluştu");
            }

            return RedirectToAction("Login");
        }





        [HttpPost]
        public async Task<IActionResult> InitializeCustomers()
        {
            try
            {
                // eğer veritabanında müşteri varsa bu işlem tekrar çalışmayacak
                if (_context.Customers.Any())
                {
                    return Content("Müşteri verileri zaten mevcut.");
                }

                var random = new Random();
                var customers = new List<Customer>();

                // Premium müşteriler (En az 2 adet)
                for (int i = 1; i <= 2; i++)
                {
                    customers.Add(new Customer
                    {
                        CustomerName = $"user{i}",
                        Budget = random.Next(500, 3001),
                        CustomerType = "Premium",
                        TotalSpent = 0,
                        Username = $"user{i}",
                        LastName = $"last{i}",
                        Email = $"user{i}@example.com",
                        Password = HashHelper.HashPassword("123"),
                        Gender = "Male"
                    });
                }

                // standard müşteriler (3-8 arasında rastgele sayıda)
                int standardCustomerCount = random.Next(3, 9);
                for (int i = 3; i < 3 + standardCustomerCount; i++)
                {
                    customers.Add(new Customer
                    {
                        CustomerName = $"user{i}",
                        Budget = random.Next(500, 3001),
                        CustomerType = "Standard",
                        TotalSpent = 0,
                        Username = $"user{i}",
                        LastName = $"last{i}",
                        Email = $"user{i}@example.com",
                        Password = HashHelper.HashPassword("123"),
                        Gender = "Female"
                    });
                }

                // müşterileri veritabanına ekle
                _context.Customers.AddRange(customers);
                await _context.SaveChangesAsync();

                return Content($"{customers.Count} müşteri başarıyla oluşturuldu. Örnek: user1, user2 ...");
            }
            catch (Exception ex)
            {
                return Content($"Müşteri oluşturma sırasında bir hata oluştu: {ex.Message}");
            }
        }



        public IActionResult Dashboard()
        {

            return View();
        }


    }
}