
using Microsoft.EntityFrameworkCore;
using StokYönetimi.Models;

namespace StokYönetimi.Data


{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

       
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Admin> Admins { get; set; }

        public DbSet<ShoppingCart> ShoppingCart { get; set; }


    }
}
