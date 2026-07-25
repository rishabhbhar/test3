using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId)
                      .HasColumnName("user_id")
                      .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.Username)
                      .HasColumnName("username")
                      .HasMaxLength(100)
                      .IsRequired();

                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.PasswordHash)
                      .HasColumnName("password_hash")
                      .IsRequired();

                entity.Property(e => e.Role)
                      .HasColumnName("role")
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(e => e.IsActive)
                      .HasColumnName("is_active")
                      .HasDefaultValue(true)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();

                entity.ToTable(t => t.HasCheckConstraint("chk_user_role", "role IN ('ADMIN', 'USER')"));
            });
        }
    }
}
