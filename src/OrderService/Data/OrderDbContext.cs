using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");

                entity.HasKey(e => e.OrderId);

                entity.Property(e => e.OrderId)
                      .HasColumnName("order_id")
                      .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.UserId)
                      .HasColumnName("user_id")
                      .IsRequired();

                entity.Property(e => e.OrderStatus)
                      .HasColumnName("order_status")
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();

                entity.ToTable(t => t.HasCheckConstraint(
                    "chk_order_status", "order_status IN ('CREATED', 'CONFIRMED', 'CANCELLED')"));

                entity.HasMany(e => e.Items)
                      .WithOne(e => e.Order)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");

                entity.HasKey(e => e.OrderItemId);

                entity.Property(e => e.OrderItemId)
                      .HasColumnName("order_item_id")
                      .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.OrderId)
                      .HasColumnName("order_id")
                      .IsRequired();

                entity.Property(e => e.ProductId)
                      .HasColumnName("product_id")
                      .IsRequired();

                entity.Property(e => e.Quantity)
                      .HasColumnName("quantity")
                      .IsRequired();

                entity.ToTable(t => t.HasCheckConstraint("chk_order_item_quantity", "quantity > 0"));
            });
        }
    }
}
