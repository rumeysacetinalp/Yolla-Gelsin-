using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokYönetimi.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using StokYönetimi.Data;
using System.Linq;



using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using StokYönetimi.StokYönetimi.Service;


namespace StokYönetimi.Controllers
{
    public class AdminController : Controller
    {

        private readonly AppDbContext _context; 
        private readonly IWebHostEnvironment _environment;
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        public static readonly ConcurrentDictionary<int, SemaphoreSlim> ProductLocks = new();
        private static readonly SemaphoreSlim CustomerSemaphore = new SemaphoreSlim(1, 1);


        private readonly ILogger<AdminController> _logger;
        private readonly LogService _logService;


        public AdminController(AppDbContext context, IWebHostEnvironment environment, ILogger<AdminController> logger, LogService logService)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _logService = logService;
        }


        public IActionResult admin()
        {
            return View();
        }


        public IActionResult ProductAdd()
        {
            return View("ProductAdd");
        }

        public IActionResult TestDb()
        {
            try
            {
                bool canConnect = _context.Database.CanConnect(); // Veritabanı bağlantısını test eder
                if (canConnect)
                {
                    return Content("Veritabanı bağlantısı başarılı.");
                }
                else
                {
                    return Content("Veritabanına bağlanılamadı.");
                }
            }
            catch (Exception ex)
            {
                return Content($"Bağlantı hatası: {ex.Message}");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ÜrünEkleme(Product model, IFormFile productImage)
        {
            try
            {
                string? filePath = null;

           
                if (productImage != null && productImage.Length > 0)
                {
                    var fileName = Path.GetFileName(productImage.FileName);
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                    
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    filePath = Path.Combine("/uploads", fileName);
                    var fullPath = Path.Combine(uploads, fileName);

                   
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await productImage.CopyToAsync(stream);
                    }

                  
                    model.ProductImage = filePath;
                    Console.WriteLine("Kaydedilen dosya yolu: " + filePath); 
                }
                else
                {
                    ModelState.AddModelError("ProductImage", "Lütfen bir resim dosyası yükleyin.");
                    return View("ProductAdd", model);
                }

                // Veriyi veritabanına kaydet
                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                // **Başarı durumunda log ekleme**
                _logService.AddLog(
                    customerId: 49, 
                    logType: "Bilgilendirme",
                    logDetails: $"Yeni ürün eklendi: {model.ProductName} (ID: {model.ProductID})"
                );

                TempData["SuccessMessage"] = "Ürün başarıyla eklendi!";
                return RedirectToAction("ProductAdd");
            }
            catch (Exception ex)
            {
                // **Hata durumunda log ekleme**
                _logService.AddLog(
                    customerId: 49, 
                    logType: "Hata",
                    logDetails: $"Ürün eklerken bir hata oluştu: {ex.Message}"
                );

                TempData["ErrorMessage"] = "Ürün eklerken bir hata oluştu: " + ex.Message;
                return View("ProductAdd", model);
            }
        }



        // Ürünleri Listeleme
        public IActionResult ÜrünListe()
        {

            try
            {
                var products = _context.Products
                    .Where(p => p.ProductName != null && p.Price != null)
                    .ToList();
                return View(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
                TempData["ErrorMessage"] = "Ürünler listelenirken bir hata oluştu: " + ex.Message;
                return View(new List<Product>()); 
            }


        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ÜrünGüncelle(Product model, IFormFile productImage)
        {
            _logger.LogInformation($"[Ürün Güncelleme] ÜrünID: {model.ProductID} güncelleniyor...");

            if (!ProductLocks.ContainsKey(model.ProductID))
            {
                ProductLocks[model.ProductID] = new SemaphoreSlim(1, 1);
            }

            var productSemaphore = ProductLocks[model.ProductID];

            // Kilidi kontrol et
            if (productSemaphore.CurrentCount == 0)
            {
                TempData["ErrorMessage"] = "Bu ürün şu anda başka bir işlem tarafından kilitlenmiş durumda. Lütfen 2 dakika sonra tekrar deneyin.";
                return RedirectToAction("ÜrünSilme");
            }

            try
            {
                await productSemaphore.WaitAsync(); // Ürünü güncellemek için kilit al

                var product = await _context.Products.FindAsync(model.ProductID);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Ürün bulunamadı!";
                    return RedirectToAction("ÜrünSilme");
                }

                // Ürün bilgilerini güncelle
                product.ProductName = model.ProductName;
                product.Stock = model.Stock;
                product.Price = model.Price;
                product.Category = model.Category;

                // Yeni resim yükleme işlemi
                if (productImage != null)
                {
                    var fileName = Path.GetFileName(productImage.FileName);
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    var filePath = Path.Combine("/uploads", fileName);
                    var fullPath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await productImage.CopyToAsync(stream);
                    }

                    product.ProductImage = filePath;
                }

                await _context.SaveChangesAsync(); // Güncellemeyi veritabanına kaydet

                // **Başarı Durumunda Log Ekleme**
                _logService.AddLog(
                    customerId: 49,
                    logType: "Bilgilendirme",
                    logDetails: $"Ürün güncellendi: {model.ProductName} (ID: {model.ProductID})"
                );

                _logger.LogInformation($"[Ürün Güncelleme] ÜrünID: {model.ProductID} başarıyla güncellendi.");

                TempData["SuccessMessage"] = "Ürün başarıyla güncellendi!";
            }
            catch (Exception ex)
            {
                // **Hata Durumunda Log Ekleme**
                _logService.AddLog(
                    customerId: 49,
                    logType: "Hata",
                    logDetails: $"Ürün güncellenirken bir hata oluştu: {ex.Message}"
                );

                _logger.LogError(ex, $"[Ürün Güncelleme] ÜrünID: {model.ProductID} güncellenirken bir hata oluştu.");
                TempData["ErrorMessage"] = $"Ürün güncellenirken bir hata oluştu: {ex.Message}";
            }
            finally
            {
                // 2 Dakikalık Bekleme Süresi
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(2)); // 2 dakika bekle
                    productSemaphore.Release(); // Kilidi serbest bırak
                    _logger.LogInformation($"[Ürün Güncelleme] ÜrünID: {model.ProductID} için kilit otomatik olarak serbest bırakıldı.");
                });
            }

            return RedirectToAction("ÜrünSilme");
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (ProductLocks.ContainsKey(id) && ProductLocks[id].CurrentCount == 0)
            {
                TempData["ErrorMessage"] = "Bu ürün şu anda başka bir işlem tarafından kilitlenmiş durumda. Lütfen 2 dakika sonra tekrar deneyin.";
                return RedirectToAction("ÜrünSilme");
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Silinmek istenen ürün bulunamadı.";
                    return RedirectToAction("ÜrünSilme");
                }

                // Orders tablosunda bu ürünle alakalı beklemede olan siparişleri buluyor
                var pendingOrders = _context.Orders
                    .Where(o => o.ProductID == id && o.OrderStatus == "Beklemede")
                    .ToList();

                foreach (var order in pendingOrders)
                {
                    order.OrderStatus = "Reddedildi";

                    _logService.AddLog(
                        customerId: order.CustomerID,
                        logType: "Hata",
                        logDetails: $"Ürün silindiği için sipariş reddedildi: SiparişID: {order.OrderID}, ÜrünID: {order.ProductID}, MüşteriID: {order.CustomerID}."
                    );
                }

                // Ürünü sil
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                // **Başarı Durumunda Log Ekleme**
                _logService.AddLog(
                    customerId: 49,
                    logType: "Bilgilendirme",
                    logDetails: $"Ürün silindi: {product.ProductName} (ID: {product.ProductID})"
                );

                TempData["SuccessMessage"] = "Ürün başarıyla silindi ve ilgili beklemedeki siparişler reddedildi.";
            }
            catch (Exception ex)
            {
                // **Hata Durumunda Log Ekleme**
                _logService.AddLog(
                    customerId: 49,
                    logType: "Hata",
                    logDetails: $"Ürün silinirken bir hata oluştu: {ex.Message}"
                );

                TempData["ErrorMessage"] = $"Ürün silinirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("ÜrünSilme");
        }






        public IActionResult ÜrünSilme()
        {
            var products = _context.Products.ToList();
            return View(products);
        }




        [HttpPost]
        public async Task<IActionResult> LockProduct(int productId)
        {
            // Eğer ürün zaten kilitliyse
            if (ProductLocks.ContainsKey(productId))
            {
                return Conflict("Bu ürün şu anda başka bir admin tarafından düzenleniyor.");
            }

            // Yeni bir semaphore ekle ve kilit al
            var semaphore = new SemaphoreSlim(1, 1);
            ProductLocks.TryAdd(productId, semaphore);

            await semaphore.WaitAsync(); // Kilidi al
            _logger.LogInformation($"Ürün {productId} kilitlendi.");

            // 2 dakika sonra kilidi serbest bırak
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(2)); // 2 dakika bekle
                ProductLocks.TryRemove(productId, out var removedSemaphore);
                removedSemaphore?.Release(); // Semaphore'u serbest bırak
                _logger.LogInformation($"Ürün {productId} için kilit otomatik olarak serbest bırakıldı.");
            });

            return Ok("Ürün başarıyla kilitlendi.");
        }




[Route("Admin/GetAllLogs")]
[HttpGet]
public IActionResult GetAllLogs()
{
    try
    {
        var logs = _context.Logs
            .Select(log => new
            {
                log.LogDate,
                log.LogType,
                log.LogDetails
            })
            .ToList();

        return Json(logs);
    }
    catch (Exception ex)
    {
        return BadRequest($"Hata: {ex.Message}");
    }
}

        public IActionResult Logs()
        {
            return View(); 
        }



        [HttpPost]
        public IActionResult UnlockProduct(int productId)
        {
            if (ProductLocks.TryRemove(productId, out var semaphore))
            {
                semaphore.Release(); // Kilidi serbest bırak
                _logger.LogInformation($"[Ürün Güncelleme] ÜrünID: {productId} manuel olarak serbest bırakıldı.");
                return Ok("Ürün kilidi başarıyla serbest bırakıldı.");
            }

            return BadRequest("Ürün zaten kilitli değil veya kilit size ait değil.");
        }

        
        public IActionResult SiparisOnay()
        {
            var now = DateTime.Now;

            // Önce veritabanından verileri çekiyoruz (Bellekte işlem yapmak için)
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .Where(o => o.OrderStatus == "Beklemede")
                .ToList(); // Sorguyu burada sonlandırıyoruz ve verileri belleğe alıyoruz

            // Bellekte dinamik öncelik hesaplaması yapıyoruz
            var sortedOrders = orders.Select(o => new
            {
                Order = o,
                WaitTime = (now - o.OrderDate).TotalSeconds, // Bekleme süresi 
                PriorityScore = (o.Customer.CustomerType == "Premium" ? 15 : 10) +
                                ((now - o.OrderDate).TotalSeconds * 0.5) // Dinamik hesaplama
            })
            .OrderByDescending(o => o.PriorityScore) // Öncelik puanına göre sıralama
            .Select(o => o.Order) // Sadece Order nesnelerini döndürüyoruz
            .ToList();

            return View(sortedOrders); // viewe sıralı listeyi gönderiyoruz
        }


        //buton

        [HttpPost]
        public IActionResult SiparisOnayla()
        {
            var now = DateTime.Now;

            // Siparişleri veritabanından çekiyoruz
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .Where(o => o.OrderStatus == "Beklemede")
                .ToList(); // Sorguyu burada sonlandırıyoruz ve verileri belleğe alıyoruz

            // Bellekte dinamik öncelik hesaplaması yapıyoruz
            var sortedOrders = orders.Select(o => new
            {
                Order = o,
                WaitTime = (now - o.OrderDate).TotalSeconds, // Bekleme süresi 
                PriorityScore = (o.Customer.CustomerType == "Premium" ? 15 : 10) +
                                ((now - o.OrderDate).TotalSeconds * 0.5) // Dinamik hesaplama
            })
            .OrderByDescending(o => o.PriorityScore) // Öncelik puanına göre sıralama
            .Select(o => o.Order) // Sadece Order nesnelerini döndürüyoruz
            .ToList();

            // Stok kontrolü ve güncelleme
            foreach (var order in sortedOrders)
            {
                var product = _context.Products.Find(order.ProductID);
                var user = _context.Customers.Find(order.CustomerID);

                if (product.Stock >= order.Quantity)
                {
                    // Stok yeterliyse siparişi onayla ve stok güncelle
                    product.Stock -= order.Quantity;
                    user.Budget -= order.TotalPrice;
                    user.TotalSpent = (user.TotalSpent ?? 0) + order.TotalPrice;
                    order.OrderStatus = "Onaylandı";

                    _logService.AddLog(order.CustomerID, "Bilgilendirme",
                        $"Müşteri {user.CustomerName} ({user.CustomerID}), {product.ProductID} ID'li ve {product.ProductName} isimli üründen {order.Quantity} adet sipariş verdi ve siparişi onaylandı.");
                }
                else
                {
                    // Stok yetersizse siparişi reddet
                    order.OrderStatus = "Reddedildi";

                    _logService.AddLog(order.CustomerID, "Hata",
                        $"Ürün stoğu yetersiz: {product.ProductName}");
                }

                // Müşteri türü değişikliğini kontrol et (Semaphore kullanarak threadsafe yapı)
                if (user.CustomerType == "Standard" && (user.TotalSpent ?? 0) > 2000)
                {
                    Task.Run(async () =>
                    {
                        await CustomerSemaphore.WaitAsync(); // Semafor ile threadsafe erişim sağlanır
                        try
                        {
                            user.CustomerType = "Premium";

                            _logService.AddLog(user.CustomerID, "Bilgilendirme",
                                $"Müşteri {user.CustomerName} ({user.CustomerID}) Standard türden Premium türe yükseltildi.");
                        }
                        finally
                        {
                            CustomerSemaphore.Release(); // Erişim serbest bırakılır
                        }
                    });
                }
            }

            _context.SaveChanges(); // Veritabanına işlemleri kaydet
            TempData["SuccessMessage"] = "Tüm siparişler başarıyla işlendi!";
            return RedirectToAction("SiparisOnay");
        }


        [HttpGet]
        public IActionResult GetSortedOrders()
        {
            var now = DateTime.Now;
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .Where(o => o.OrderStatus == "Beklemede")
                .ToList()
                .Select(o => new
                {
                    CustomerName = o.Customer.CustomerName,
                    ProductName = o.Product.ProductName,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate.ToString("g"), // Tarihi uygun formatta gönder
                    WaitTime = (now - o.OrderDate).TotalSeconds,
                    CustomerType = o.Customer.CustomerType,
                    PriorityScore = (o.Customer.CustomerType == "Premium" ? 15 : 10) +
                                    ((now - o.OrderDate).TotalSeconds * 0.5)
                })
                .OrderByDescending(o => o.PriorityScore);

            return Json(orders);
        }





        public async Task<IActionResult> UpdateStockWithLock(int productId, int quantity)
        {
            if (!ProductLocks.ContainsKey(productId))
            {
                ProductLocks[productId] = new SemaphoreSlim(1, 1);
            }

            var semaphore = ProductLocks[productId];

            try
            {
                await semaphore.WaitAsync();

                var product = _context.Products.Find(productId);
                if (product.Stock < quantity)
                {
                    return BadRequest("Yetersiz stok.");
                }

                product.Stock -= quantity;
                await _context.SaveChangesAsync();

                return Ok("Stok başarıyla güncellendi.");
            }
            finally
            {
                semaphore.Release();
            }
        }



        public IActionResult StokGoruntule()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        public IActionResult GetAvailableProducts()
        {
            var availableProducts = _context.Products
                .Where(p => !ProductLocks.ContainsKey(p.ProductID)) // Kilitli olmayan ürünler
                .ToList();

            return View(availableProducts);
        }




    }
}
