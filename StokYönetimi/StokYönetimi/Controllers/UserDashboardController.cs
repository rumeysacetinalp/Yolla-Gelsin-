using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StokYönetimi.Data;
using StokYönetimi.Models;
using System.Linq;
using StokYönetimi.Controllers;
using StokYönetimi.StokYönetimi.Service;

namespace StokYonetimi.Controllers
{
    public class UserDashboardController : Controller
    {
        private readonly ILogger<UserDashboardController> _logger;
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public UserDashboardController(ILogger<UserDashboardController> logger, AppDbContext context, LogService logService)
        {
            _logger = logger;
            _context = context;
            _logService = logService;
        }

        public async Task<IActionResult> Dashboard(string category = null)
        {
            // Kilitli ürünlerin IDlerini al
            var lockedProductIds = AdminController.ProductLocks.Keys.ToList();

            // Ürün kategorilerini al
            var categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;

            // Ürünleri getir ve kilitli ürünleri hariç tut
            var products = string.IsNullOrEmpty(category)
                ? await _context.Products
                    .Where(p => !lockedProductIds.Contains(p.ProductID))
                    .ToListAsync()
                : await _context.Products
                    .Where(p => p.Category == category && !lockedProductIds.Contains(p.ProductID))
                    .ToListAsync();

            
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            var cartItems = userId == null
                ? new List<ShoppingCart>()
                : await _context.ShoppingCart
                    .Where(c => c.CustomerID == userId)
                    .Include(c => c.Product)
                    .ToListAsync();

          
            ViewBag.LockedProductIds = lockedProductIds;

            return View(Tuple.Create(products, cartItems));
        }




        // Profil Sayfası
        public async Task<IActionResult> Profil()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            _logger.LogInformation($"Profil/Dashboard için Session KullaniciID: {userId}");

            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = await _context.Customers.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            var orders = await _context.Orders
                .Where(o => o.CustomerID == userId)
                .Include(o => o.Product)
                .ToListAsync();

            ViewData["UserName"] = user.CustomerName + " " + user.LastName;
            ViewBag.Orders = orders;

            return View(user);
        }

        // Profil Güncelleme
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string customerName, string lastName, string email)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Oturum geçerli değil. Lütfen tekrar giriş yapınız.";
                return RedirectToAction("Login", "Home");
            }

            var user = await _context.Customers.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Profil");
            }

            user.CustomerName = customerName;
            user.LastName = lastName;
            user.Email = email;

            try
            {
                _context.Customers.Update(user);
                await _context.SaveChangesAsync();

              
                _logService.AddLog(
                    customerId: userId.Value,
                    logType: "Bilgilendirme",
                    logDetails: $"Müşteri {userId} ({user.CustomerType}) profil bilgilerini güncelledi: {customerName} {lastName}, {email}."
                );

                TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Database Update Error: {ex.Message}");
                TempData["ErrorMessage"] = "Profil bilgileri güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("Profil");
        }


        [HttpPost]
        public async Task<IActionResult> UpdateBudget(decimal newBudget)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Oturum geçerli değil. Lütfen tekrar giriş yapınız.";
                return RedirectToAction("Login", "Home");
            }

            var user = await _context.Customers.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Profil");
            }

            
            if (newBudget < 500 || newBudget > 3000)
            {
                TempData["ErrorMessage"] = "Bütçe 500 TL ile 3000 TL arasında olmalıdır.";
                return RedirectToAction("Profil");
            }

            user.Budget = newBudget;

            try
            {
                _context.Customers.Update(user);
                await _context.SaveChangesAsync();

                
                _logService.AddLog(
                    customerId: userId.Value,
                    logType: "Bilgilendirme",
                    logDetails: $"Müşteri {userId} ({user.CustomerType}) bütçesini {newBudget} TL olarak güncelledi."
                );

                TempData["SuccessMessage"] = "Bütçe başarıyla güncellendi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Database Update Error: {ex.Message}");
                TempData["ErrorMessage"] = "Bütçe güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("Profil");
        }



        // Şifre Güncelleme
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Oturum geçerli değil. Lütfen tekrar giriş yapınız.";
                return RedirectToAction("Login", "Home");
            }

            var user = await _context.Customers.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Profil");
            }

            if (!HashHelper.VerifyPassword(currentPassword, user.Password))
            {
                TempData["ErrorMessage"] = "Mevcut şifre yanlış.";
                return RedirectToAction("Profil");
            }

            user.Password = HashHelper.HashPassword(newPassword);

            try
            {
                _context.Customers.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Şifre başarıyla güncellendi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Database Update Error: {ex.Message}");
                TempData["ErrorMessage"] = "Şifre güncellenirken bir hata oluştu.";
            }

            return RedirectToAction("Profil");
        }

        //Sepete ürün ekleme

        [HttpPost]
        public async Task<JsonResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            if (userId == null)
            {
                return Json(new { success = false, message = "Lütfen giriş yapınız." });
            }

            var user = await _context.Customers.FindAsync(userId);
            var customerType = user.CustomerType; 

            var existingCartItem = await _context.ShoppingCart
                .FirstOrDefaultAsync(c => c.CustomerID == userId && c.ProductID == request.ProductId);

            // Maksimum 5 ürün kontrolü
            int existingQuantity = existingCartItem?.Quantity ?? 0; 
            if (existingQuantity + request.Quantity > 5)
            {
                return Json(new { success = false, message = "Sepete en fazla 5 adet ürün ekleyebilirsiniz." });
            }

            // Yeni miktar geçerli ise işlem devam eder
            if (existingCartItem != null)
            {
                existingCartItem.Quantity += request.Quantity;
            }
            else
            {
                var cartItem = new ShoppingCart
                {
                    CustomerID = userId.Value,
                    ProductID = request.ProductId,
                    Quantity = request.Quantity
                };
                _context.ShoppingCart.Add(cartItem);
            }

            try
            {
                await _context.SaveChangesAsync();

              
                var product = await _context.Products.FindAsync(request.ProductId);
                _logService.AddLog(
                    customerId: userId.Value,
                    logType: "Bilgilendirme",
                    logDetails: $"Müşteri {userId} ({customerType}), {product.ProductID} ID'li {product.ProductName} adlı üründen {request.Quantity} adet sepete ekledi."
                );

                return Json(new { success = true, message = "Ürün sepete başarıyla eklendi!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Hata: {ex.Message}");
                return Json(new { success = false, message = "Ürün sepete eklenirken bir hata oluştu." });
            }
        }



        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }


        // Sepeti Görüntüleme
        public async Task<IActionResult> GetCard()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Home");
            }

            var cartItems = await _context.ShoppingCart
                .Where(c => c.CustomerID == userId)
                .Include(c => c.Product)
                .ToListAsync();

            return View("Sepet", cartItems); 
        }


        // Sepetten Ürün Silme
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var cartItem = await _context.ShoppingCart.FindAsync(cartId);
            if (cartItem != null)
            {
                var user = await _context.Customers.FindAsync(cartItem.CustomerID);
                var product = await _context.Products.FindAsync(cartItem.ProductID);

                _context.ShoppingCart.Remove(cartItem);
                await _context.SaveChangesAsync();


                _logService.AddLog(
                    customerId: cartItem.CustomerID,
                    logType: "Bilgilendirme",
                    logDetails: $"Müşteri {cartItem.CustomerID} ({user.CustomerType}), {product.ProductID} ID'li {product.ProductName} adlı üründen {cartItem.Quantity} adet sepetten kaldırdı."
                );

                TempData["SuccessMessage"] = "Ürün sepetten kaldırıldı.";
            }

            return RedirectToAction("GetCard");
        }


        //Satın alma
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("KullaniciID");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Home");
            }

            var cartItems = await _context.ShoppingCart
                .Where(c => c.CustomerID == userId)
                .Include(c => c.Product)
                .ToListAsync();

            var user = await _context.Customers.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                return RedirectToAction("GetCard");
            }

            try
            {
                foreach (var item in cartItems)
                {
                    if (item.Product == null)
                    {
                        TempData["ErrorMessage"] = "Sepette geçersiz bir ürün var.";
                        _logService.AddLog(
                            customerId: userId.Value,
                            logType: "Hata",
                            logDetails: $"Müşteri {user.CustomerName} (ID: {user.CustomerID}) - Sepette geçersiz bir ürün bulundu."
                        );
                        return RedirectToAction("GetCard");
                    }

                    if (item.Product.Stock < item.Quantity)
                    {
                        TempData["ErrorMessage"] = "Ürün stoğu yetersiz.";
                        _logService.AddLog(
                            customerId: userId.Value,
                            logType: "Hata",
                            logDetails: $"Müşteri {user.CustomerName} (ID: {user.CustomerID}) - Ürün stoğu yetersiz: {item.Product.ProductName} (ID: {item.Product.ProductID}) için istenen miktar: {item.Quantity}, mevcut stok: {item.Product.Stock}."
                        );
                        return RedirectToAction("GetCard");
                    }

                    if (user.Budget < (item.Quantity * (item.Product.Price ?? 0)))
                    {
                        TempData["ErrorMessage"] = "Bakiyeniz yetersiz.";
                        _logService.AddLog(
                            customerId: userId.Value,
                            logType: "Hata",
                            logDetails: $"Müşteri {user.CustomerName} (ID: {user.CustomerID}) - Bakiye yetersiz: Gerekli bakiye: {item.Quantity * (item.Product.Price ?? 0)}, mevcut bakiye: {user.Budget}."
                        );
                        return RedirectToAction("GetCard");
                    }

                    // Sipariş tablosuna ekleme
                    var order = new Order
                    {
                        CustomerID = userId.Value,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        TotalPrice = item.Quantity * (item.Product?.Price ?? 0),
                        OrderDate = DateTime.Now,
                        OrderStatus = "Beklemede"
                    };
                    _context.Orders.Add(order);

                    // Loglama
                    string customerType = string.IsNullOrWhiteSpace(user.CustomerType) ? "belirtilmedi" : user.CustomerType;
                    _logService.AddLog(
                        customerId: userId.Value,
                        logType: "Bilgilendirme",
                        logDetails: $"Müşteri {user.CustomerName} (ID: {user.CustomerID}, Tür: {customerType}) - {item.ProductID} ID'li {item.Product?.ProductName ?? "Ürün adı belirtilmedi"} ürününden {item.Quantity} adet sipariş verdi."
                    );
                }

                // Sepeti temizle
                _context.ShoppingCart.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Siparişiniz başarıyla alındı.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                // Veritabanı hatası loglama
                _logService.AddLog(
                    customerId: userId.Value,
                    logType: "Hata",
                    logDetails: $"Müşteri {user.CustomerName} (ID: {user.CustomerID}) - Veritabanı Hatası: {ex.Message}"
                );
                TempData["ErrorMessage"] = "Bir hata oluştu, lütfen tekrar deneyiniz.";
                return RedirectToAction("GetCard");
            }
        }





        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Arama sorgusu boş olamaz.");
            }

            // Arama işlemini gerçekleştir
            var results = await _context.Products
                .Where(p => p.ProductName.Contains(query) || p.Category.Contains(query))
                .Select(p => new { p.ProductID, p.ProductName, p.Category })
                .ToListAsync();

            return Ok(results);
        }


    }
}