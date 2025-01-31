namespace StokYönetimi
{
    using global::StokYönetimi.Data;
    using global::StokYönetimi.Models;
    using System;
    using System.Threading;
  

    namespace StokYönetimi.Service
    {
        public class LogService
        {
            private readonly AppDbContext _context;
            private static readonly Semaphore LogSemaphore = new Semaphore(1, 1); // 1 thread için izin

            public LogService(AppDbContext context)
            {
                _context = context;
            }

            public void AddLog(int customerId, string logType, string logDetails, int? orderId = null)
            {
                // Kilit al
                LogSemaphore.WaitOne();
                try
                {
                    // Log işlemi
                    var log = new Log
                    {
                        CustomerID = customerId,
                        OrderID = orderId,
                        LogType = logType,
                        LogDetails = logDetails,
                        LogDate = DateTime.Now
                    };

                    // Veritabanına ekleme
                    _context.Logs.Add(log);
                    _context.SaveChanges(); // Senkron olarak kaydediyoruz
                }
                finally
                {
                    // Kilidi serbest bırak
                    LogSemaphore.Release();
                }
            }

         


        }
    }

}
