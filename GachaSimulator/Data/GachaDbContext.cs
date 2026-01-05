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

            //modelBuilder.Entity<UserPityState>(entity =>
            //{
            //    entity.Property(e => e.BannerType).HasConversion<string>();
            //});

            modelBuilder.Entity<Banner>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<string>();
            });

            modelBuilder.Entity<WishHistory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnType("char(36)");
            });
        }
    }
}
