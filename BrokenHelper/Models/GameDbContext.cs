using Microsoft.EntityFrameworkCore;

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

        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
