using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("products");

                entity.HasKey(e => e.ProductId);

                entity.Property(e => e.ProductId)
                      .HasColumnName("product_id")
                      .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.ProductName)
                      .HasColumnName("product_name")
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(e => e.StockQty)
                      .HasColumnName("stock_qty")
                      .IsRequired();

                entity.Property(e => e.IsActive)
                      .HasColumnName("is_active")
                      .HasDefaultValue(true)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();

                entity.Property(e => e.UpdatedAt)
                      .HasColumnName("updated_at");

                entity.ToTable(t => t.HasCheckConstraint("chk_stock_qty_non_negative", "stock_qty >= 0"));
            });
        }
    }
}
