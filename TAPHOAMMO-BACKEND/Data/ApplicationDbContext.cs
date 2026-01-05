using Microsoft.EntityFrameworkCore;
using TAPHOAMMO_BACKEND.Models;

namespace TAPHOAMMO_BACKEND.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<ProductOption> ProductOptions { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<UserBankInfo> UserBankInfos { get; set; }
        public DbSet<UserAuthInfo> UserAuthInfos { get; set; }
        public DbSet<ProductStats> ProductStats { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<VatInvoiceInfo> VatInvoiceInfos { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Seller)
                    .WithMany(u => u.Products)
                    .HasForeignKey(p => p.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ProductCode).IsUnique();
            });

            // Review configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Một user chỉ có thể đánh giá một sản phẩm một lần
                entity.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
            });

            // OtpCode configuration
            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => new { e.Email, e.Code, e.IsUsed });
            });

            // ProductOption configuration
            modelBuilder.Entity<ProductOption>(entity =>
            {
                entity.HasOne(o => o.Product)
                    .WithMany(p => p.Options)
                    .HasForeignKey(o => o.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => e.ProductId);
            });

            // CartItem configuration
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Product)
                    .WithMany()
                    .HasForeignKey(c => c.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ProductOption)
                    .WithMany()
                    .HasForeignKey(c => c.ProductOptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 1 user chỉ có 1 cart item cho 1 product+option combination
                entity.HasIndex(c => new { c.UserId, c.ProductId, c.ProductOptionId }).IsUnique();
            });

            // UserBankInfo configuration - One-to-One relationship with User
            modelBuilder.Entity<UserBankInfo>(entity =>
            {
                entity.HasOne(b => b.User)
                    .WithOne(u => u.BankInfo)
                    .HasForeignKey<UserBankInfo>(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId).IsUnique(); // Mỗi user chỉ có 1 bank info
            });

            // UserAuthInfo configuration - One-to-One relationship with User
            modelBuilder.Entity<UserAuthInfo>(entity =>
            {
                entity.HasOne(a => a.User)
                    .WithOne(u => u.AuthInfo)
                    .HasForeignKey<UserAuthInfo>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId).IsUnique(); // Mỗi user chỉ có 1 auth info
                entity.HasIndex(e => e.GoogleId); // Index cho GoogleId để tìm kiếm nhanh
            });

            // ProductStats configuration - One-to-One relationship with Product
            modelBuilder.Entity<ProductStats>(entity =>
            {
                entity.HasOne(s => s.Product)
                    .WithOne(p => p.Stats)
                    .HasForeignKey<ProductStats>(s => s.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId).IsUnique(); // Mỗi product chỉ có 1 stats
            });

            // Wallet configuration - One-to-One relationship with User
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasOne(w => w.User)
                    .WithOne()
                    .HasForeignKey<Wallet>(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId).IsUnique(); // Mỗi user chỉ có 1 wallet
                entity.HasIndex(e => e.DepositCode); // Index cho mã nạp tiền để Senpay check trùng
            });

            // WalletTransaction configuration
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Seller)
                    .WithMany()
                    .HasForeignKey(t => t.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Product)
                    .WithMany()
                    .HasForeignKey(t => t.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.RelatedTransaction)
                    .WithMany()
                    .HasForeignKey(t => t.RelatedTransactionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SellerId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ReportDeadline);
            });

            // VatInvoiceInfo configuration
            modelBuilder.Entity<VatInvoiceInfo>(entity =>
            {
                entity.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsDefault });
            });

            // ActivityLog configuration
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull); // Set null khi user bị xóa để giữ lại log

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Operation);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.UserId, e.Timestamp });
            });
        }
    }
}

