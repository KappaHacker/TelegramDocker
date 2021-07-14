using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TelegramBotUser;
using ProgramSettings;
using Microsoft.Extensions.Logging;

namespace TelegramDocker
{
    public class ApplicationContext : DbContext
    {
        public DbSet<TelegramUser> TelegramChatInfo { get; set; }

        public ApplicationContext()
        {
            try
            {
                Database.EnsureCreated();
                System.Console.WriteLine("Подключение к БД успешно");
            }
            catch(System.Exception ex)
            {
                UserDataManager.ErrorMessage(ex, Program.logger);
                Program.logger.LogInformation("Хранение данных будет происходить в JSON файл");
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Configuration.conectionString);
            optionsBuilder.LogTo(System.Console.WriteLine, new[] { RelationalEventId.CommandExecuted});
            optionsBuilder.EnableDetailedErrors();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}