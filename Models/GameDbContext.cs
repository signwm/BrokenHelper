using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BrokenHelper.Models
{
    public class GameDbContext : DbContext
    {
        public DbSet<InstanceEntity> Instances { get; set; }
        public DbSet<FightEntity> Fights { get; set; }
        public DbSet<PlayerEntity> Players { get; set; }
        public DbSet<OpponentTypeEntity> OpponentTypes { get; set; }
        public DbSet<FightOpponentEntity> FightOpponents { get; set; }
        public DbSet<FightPlayerEntity> FightPlayers { get; set; }
        public DbSet<DropEntity> Drops { get; set; }
        public DbSet<ItemPriceEntity> ItemPrices { get; set; }
        public DbSet<ArtifactPriceEntity> ArtifactPrices { get; set; }

        public GameDbContext()
        {
        }

        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                Directory.CreateDirectory("data");
                var dbPath = Path.Combine("data", "data.db");
                var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                };
                optionsBuilder.UseSqlite(builder.ToString());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InstanceEntity>()
                .HasIndex(i => i.PublicId)
                .IsUnique();

            modelBuilder.Entity<ItemPriceEntity>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<ArtifactPriceEntity>()
                .HasIndex(p => p.Code)
                .IsUnique();
            modelBuilder.Entity<ArtifactPriceEntity>()
                .HasIndex(p => p.Name)
                .IsUnique();

        }
    }
}
