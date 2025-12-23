using Microsoft.EntityFrameworkCore;
using GachaSimulator.Models;

namespace GachaSimulator.Data
{
    public class GachaDbContext : DbContext
    {
        //Configure DbContext to use options
        public GachaDbContext(DbContextOptions<GachaDbContext> options) : base(options)
        {
        }

        public DbSet<Items> Items { get; set; }
        public DbSet<WishHistory> WishHistories { get; set; }
        public DbSet<UserPityState> UserPityState { get; set; } = null!;
        public DbSet<Banner> Banners { get; set; } = null!;
        public DbSet<BannerRateUp> BannerRateUps { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Items>(entity =>
            {
                //HasConversion enum to string for ItemType and ElementType
                entity.Property(e => e.Type)
                      .HasConversion<string>();
                entity.Property(e => e.Element)
                      .HasConversion<string>();
            });

            //Configure UserPityState BannerType as string
            modelBuilder.Entity<UserPityState>(entity =>
            {
                entity.Property(e => e.BannerType).HasConversion<string>();
            });

            //Configure Banner as string
            modelBuilder.Entity<Banner>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<string>();
            });

            //Configure WishHistory Id as char(36)
            modelBuilder.Entity<WishHistory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnType("char(36)");
            });
        }
    }
}
