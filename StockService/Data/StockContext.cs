using Microsoft.EntityFrameworkCore;
     using StockService.Models;
     using Shared; // Adiciona o namespace do projeto Shared

     namespace StockService.Data
     {
         public class StockContext : DbContext
         {
             public StockContext(DbContextOptions<StockContext> options) : base(options) { }

             public DbSet<Product> Products { get; set; }
             public DbSet<User> Users { get; set; }

             protected override void OnModelCreating(ModelBuilder modelBuilder)
             {
                 modelBuilder.Entity<Product>()
                     .Property(p => p.Price)
                     .HasPrecision(18, 2);

                 modelBuilder.Entity<User>()
                     .HasIndex(u => u.Username)
                     .IsUnique(); // Garante que o Username seja único
             }
         }
     }