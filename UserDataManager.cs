using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProgramSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelegramBotUser;

namespace TelegramDocker
{
    static class UserDataManager
    {
        public static List<TelegramUser> TUsers = new List<TelegramUser>();     //лист, в котором хранится информация о чате
        static bool dbIsWorking = true;
        static ApplicationContext db;

        public static async Task SaveChanges(long tmpChatId, TelegramUser user, ILogger logger, CancellationToken cancellationToken)
        {
            if(dbIsWorking)
            {
                try
                {
                    await SaveUsersDB(tmpChatId, user, logger);
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

        static  async Task SaveUsersDB(long tmpChatId, TelegramUser user, ILogger logger)
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

        public static async Task CheckUser(long tmpChatId, ILogger logger, CancellationToken cancellationToken)
        {

            if (TUsers.Find(n => n.Id == tmpChatId) == null)
            {
                TelegramUser user1 = new TelegramUser { migration = 0, Id = tmpChatId, setValueCheck = false, setterValueId = 0, messageId = 0 };
                TUsers.Add(user1);
                await SaveChanges(tmpChatId, user1, logger, cancellationToken);                
            }
        }
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
