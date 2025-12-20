using InventoryApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Data
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryItem>()
                .HasKey(ii => new { ii.PlayerId, ii.ItemId });

            modelBuilder.Entity<Player>()
                .HasMany(p => p.Inventory)
                .WithOne(ii => ii.Player)
                .HasForeignKey(ii => ii.PlayerId);

            modelBuilder.Entity<Item>()
                .HasMany(i => i.InventoryItems)
                .WithOne(ii => ii.Item)
                .HasForeignKey(ii => ii.ItemId);
        }
    }
}
