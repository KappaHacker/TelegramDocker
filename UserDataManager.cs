using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProgramSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramBotUser;

namespace TelegramDocker
{
    static class UserDataManager
    {
        public static List<TelegramUser> TUsers = new List<TelegramUser>();     //лист, в котором хранится информация о чате
        static bool dbIsWorking = true;                                         //состояние соединения с бд
        static ApplicationContext db;                                           //экземпляр класса контекста

        public static async Task SaveChanges(long tmpChatId, TelegramUser user, ILogger logger, CancellationToken cancellationToken)
        {
            //если соединение не было потеряно то пытаемся сохранить значения в бд и json файл
            if(dbIsWorking)
            {
                try
                {
                    await SaveUserDB(tmpChatId, user, logger);
                }
                catch(Exception ex)
                {
                    ErrorMessage(ex, logger);
                    dbIsWorking = false;
                }
               await SaveUsersJSON();
            }
            else
            {
                // если соединение было потеряно, то создаем новый экземлпяр контекста, который попытаеся создать новое соединение
                try
                {
                    db = new ApplicationContext();
                    db.AddRange(TUsers, cancellationToken);
                    db.SaveChanges();
                    dbIsWorking = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage(ex, logger);
                    dbIsWorking = false;
                }
                await SaveUsersJSON();
            }

        }

        //функция обновления записи с id чата = tmpChatId в бд
        static async Task SaveUserDB(long tmpChatId, TelegramUser user, ILogger logger)
        {
            var entity = db.telegramChatInfo.FirstOrDefault(item => item.Id == tmpChatId);
            if (entity != null)
            {
                entity.migration = user.migration;
                entity.setValueCheck = user.setValueCheck;
                entity.setterValueId = user.setterValueId;
                db.SaveChanges();
                await SaveUsersJSON();
            }
           
        }
        static async Task SaveUsersJSON()
        {
            string jsonPath = JsonConvert.SerializeObject(TUsers);
            using (StreamWriter sw = new StreamWriter(Configuration.path, false, System.Text.Encoding.Default))
            {
                await sw.WriteLineAsync(jsonPath);

            }
        }

        //проверка наличия записи по чату с id = tmpChatId, если нет то создается новая запись
        public static async Task CheckUser(long tmpChatId, ILogger logger, CancellationToken cancellationToken)
        {
            if (TUsers.Find(n => n.Id == tmpChatId) == null)
            {
                TelegramUser user1 = new TelegramUser { migration = 0, Id = tmpChatId, setValueCheck = false, setterValueId = 0, messageId = 0 };
                TUsers.Add(user1);
                await SaveChanges(tmpChatId, user1, logger, cancellationToken);                
            }
        }

        //устанвока первого соединния с бд и загрузка записей
        public static void DonwloadData( ILogger logger)
        {
            try
            {
                db = new ApplicationContext();
                TUsers = db.telegramChatInfo.ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage(ex, logger);
                TUsers = JsonConvert.DeserializeObject<List<TelegramUser>>(System.IO.File.ReadAllText(Configuration.path));
            }
        }

        private static void ErrorMessage(Exception ex, ILogger logger)
        {
            logger.LogError("LogError {0}", ex.Message);
            logger.LogInformation("StackTrace {0}", ex.StackTrace);
            logger.LogInformation("TargetSite {0}", ex.TargetSite);
        }
    }
}
