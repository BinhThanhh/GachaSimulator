using Microsoft.EntityFrameworkCore;
using GachaSimulator.Models;

namespace GachaSimulator.Data
{
    public class GachaDbContext : DbContext
    {
        public GachaDbContext(DbContextOptions<GachaDbContext> options) : base(options)
        {
        }

        public DbSet<Items> Items { get; set; }
        public DbSet<WishHistory> WishHistory { get; set; }
        public DbSet<UserPityState> UserPityState { get; set; } = null!;
        public DbSet<Banner> Banners { get; set; } = null!;
        public DbSet<BannerRateUp> BannerRateUps { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Items>(entity =>
            {
                entity.Property(e => e.Type)
                      .HasConversion<string>();
                entity.Property(e => e.Element)
                      .HasConversion<string>();
            });

            // Banner.Type được lưu như string trong database (cho Add/Edit banner)
            modelBuilder.Entity<Banner>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<string>();
            });

            // UserPityState.BannerType được lưu như integer trong database (để query nhanh)
            // Không cần conversion cho UserPityState

            modelBuilder.Entity<BannerRateUp>(entity =>
            {
                // Cấu hình relationship để sử dụng ItemId làm foreign key
                entity.HasOne(b => b.Item)
                      .WithMany()
                      .HasForeignKey(b => b.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WishHistory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnType("char(36)");
                
                // Cấu hình relationship để sử dụng ItemId làm foreign key
                entity.HasOne(w => w.Items)
                      .WithMany()
                      .HasForeignKey(w => w.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
