using Microsoft.EntityFrameworkCore;
using TelegramBotUser;

namespace TelegramDocker
{
    public class ApplicationContext : DbContext
    {
        public DbSet<TelegramUser> Users { get; set; }

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=MigrationTelegramBot;Username=postgres;Password=CyberDude");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}