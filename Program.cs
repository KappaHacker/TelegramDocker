﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProgramSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotUser;

namespace TelegramDocker
{
    public class Program
    {

        private static TelegramBotClient Bot;
        static long tmpChatId = 0;                                       //класс для работы с json файлом
        static TelegramUser user = new TelegramUser();                   //буффераня переменная, в которой будех храниться информация о чате
        static List<TelegramUser> TUsers = new List<TelegramUser>();     //лист, в котором хранится информация о чате
        static ApplicationContext db;
        static ILogger logger;
        public static async Task Main()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            logger = loggerFactory.CreateLogger<Program>();


            Bot = new TelegramBotClient(Configuration.BotToken);
            var me = await Bot.GetMeAsync();
            var cts = new CancellationTokenSource();

            try
            {
                db = new ApplicationContext();
                TUsers = db.telegramChatInfo.ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
                TUsers = JsonConvert.DeserializeObject<List<TelegramUser>>(System.IO.File.ReadAllText(Configuration.path));
            }
    
            Bot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), cts.Token);       //подключаем обработчик на обновления и ошибки

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();
        }

        // обработчик на события чата
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update != null)
            {
                //switch переключатель между задачами/апдейтами
                var handler = update.Type switch
                {
                    UpdateType.Message => BotOnMessageReceived(update, update.Message, cancellationToken),
                    UpdateType.EditedMessage => BotOnMessageReceived(update, update.EditedMessage, cancellationToken),
                    _ => UnknownUpdateHandlerAsync(update)
                };

                try
                {
                    await handler;
                }
                catch (Exception exception)
                {
                    await HandleErrorAsync(botClient, exception, cancellationToken);
                }
            }
        }

        // метод отвечающий за обработку входящего сообщения
        private static async Task BotOnMessageReceived(Update update, Message message, CancellationToken cancellationToken)
        {
            if (update.Message != null)
            {
                tmpChatId = update.Message.Chat.Id;
            }
            await CheckUser();
            user = TUsers.Find(n => n.Id == tmpChatId);
            Console.WriteLine($"Receive message type: {message.Type}");
            Message sentMessage = message;

            //проверяем является ли сообщенией командой 
            if (message.Type == MessageType.Text && message.Text.StartsWith('/'))
            {
                var action = (message.Text.Split('@').First()) switch
                {
                    "/show_migration" => ShowMigration(message),
                    "/next_migration" => NextMigration(message),
                    "/set_migration" => GetValue(message),
                    _ => Usage(message)
                };
                sentMessage = await action;
            }// или это сообщение для ввода после метод setvalue
            else if (message.Type == MessageType.Text && user.setValueCheck && user.setterValueId == message.From.Id && user.Id == message.Chat.Id)
            {
                if (message.Text.StartsWith('№'))
                {
                    var admin = Bot.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id, cancellationToken).Result;

                    if ((admin.Status != ChatMemberStatus.Administrator || admin.Status != ChatMemberStatus.Creator))
                    {

                        Console.WriteLine("зашел с сообщением - " + message.Text + " " + message.From + " " + admin.Status);

                        await setValue(message);
                    }
                }
                else
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Не удалось записать значение", replyMarkup: new ReplyKeyboardRemove());
                    TUsers.Find(n => n.Id == tmpChatId).setValueCheck = false;
                }

            }

            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");
        }

        //сохранений изменений состояния чата
        static async Task SaveUsersDB()
        {
            try
            {
                var entity = db.telegramChatInfo.FirstOrDefault(item => item.Id == tmpChatId);
                if (entity != null)
                {
                    entity.migration = user.migration;
                    entity.setValueCheck = user.setValueCheck;
                    entity.setterValueId = user.setterValueId;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
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
        // Проверка при первом запуске бота в чате
        static async Task CheckUser()
        {

            if (TUsers.Find(n => n.Id == tmpChatId) == null)
            {
                // создаем два объекта User
                TelegramUser user1 = new TelegramUser { migration = 0, Id = tmpChatId, setValueCheck = false, setterValueId = 0 };
                try
                {                 
                    // добавляем их в бд
                    db.telegramChatInfo.AddRange(user1);
                    db.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    ErrorMessage(ex);
                    TUsers.Add(user1);
                    await SaveUsersJSON();
                }
                TUsers.Add(user1);
            }

        }

        //обработка состояния чата после записи значения миграции
        public static async Task<Message> setValue(Message message)
        {
            if (long.TryParse(message.Text.TrimStart('№'), out long temp))
            {
                user.migration = temp;
                user.setValueCheck = false;
                await SaveUsersDB();
                return await Bot.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Последняя миграция - {user.migration}",
                                             replyMarkup: new ReplyKeyboardRemove()); ;
            }
            else
            {

                user.setValueCheck = false;
                return await Bot.SendTextMessageAsync(message.Chat.Id, "Не удалось записать значение", replyMarkup: new ReplyKeyboardRemove()); ;
            }
        }

        //установка состояния чата на запись значения миграции
        public static async Task<Message> GetValue(Message message)
        {
            user.setValueCheck = true;
            user.setterValueId = message.From.Id;
            Console.WriteLine($"setvaluecheck - {user.setValueCheck}");
            return await Bot.SendTextMessageAsync(message.Chat.Id, "Введите значение миграции (№.....)", replyMarkup: new ReplyKeyboardRemove());
        }

        //получение следующего знечения миграции
        static async Task<Message> NextMigration(Message message)
        {
            user.migration++;
            TUsers.Find(n => n.Id == tmpChatId).migration=user.migration;
            await SaveUsersDB();
            return await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                       text: $"Следующая миграция - {user.migration}",
                                                       replyMarkup: new ReplyKeyboardRemove());
        }

        //вывод текущео значения миграции 
        static async Task<Message> ShowMigration(Message message)
        {
            return await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                      text: $"Последняя миграция - {user.migration}",
                                                      replyMarkup: new ReplyKeyboardRemove());
        }

        //заглушка на обработчик событий в методе BotOnMessageReceived в switch
        static async Task<Message> Usage(Message message)
        {
            return await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "");
        }


        //обработка исключений
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        private static Task UnknownUpdateHandlerAsync(Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
        public static void ErrorMessage(Exception ex)
        {
            logger.LogError("LogError {0}", ex.Message);
            logger.LogInformation("StackTrace {0}", ex.StackTrace);
            logger.LogInformation("TargetSite {0}", ex.TargetSite);
        }
    }
}
