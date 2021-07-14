using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TelegramBotUser;
using ProgramSettings;

namespace TelegramDocker
{
    public class ApplicationContext : DbContext
    {
        public DbSet<TelegramUser> telegramChatInfo { get; set; }

        public ApplicationContext()
        {
            try
            {
                Database.EnsureCreated();
                System.Console.WriteLine("Подключение к БД успешно");
            }
            catch(System.Exception ex)
            {
                System.Console.WriteLine("Хранение данных будет происходить в JSON файл");
                UserDataManager.ErrorMessage(ex, Program.logger);
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