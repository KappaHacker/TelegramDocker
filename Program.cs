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

          
            UserDataManager.DonwloadData(logger);
         
    
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

            await UserDataManager.CheckUser(tmpChatId, logger, cancellationToken);
            user = UserDataManager.TUsers.Find(n => n.Id == tmpChatId);

            Console.WriteLine($"Receive message type: {message.Type}");
            Message sentMessage = message;

            //проверяем является ли сообщенией командой 
            if (message.Type == MessageType.Text && message.Text.StartsWith('/'))
            {
                var action = (message.Text.Split('@').First()) switch
                {
                    "/show_migration" => ShowMigration(message),
                    "/next_migration" => NextMigration(message, cancellationToken),
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

                        await setValue(message, cancellationToken);
                    }
                }
                else
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Не удалось записать значение", replyMarkup: new ReplyKeyboardRemove());
                    UserDataManager.TUsers.Find(n => n.Id == tmpChatId).setValueCheck = false;
                }

            }

            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");
        }

        public static async Task setValue(Message message, CancellationToken cancellationToken)
        {
            if (long.TryParse(message.Text.TrimStart('№'), out long temp))
            {
                user.migration = temp;
                user.setValueCheck = false;
                await UserDataManager.SaveChanges(tmpChatId,user ,logger, cancellationToken);

                var botMessage = await Bot.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Последняя миграция - {user.migration}",
                                             replyMarkup: new ReplyKeyboardRemove());
                if (user.messageId != 0)
                    await Bot.UnpinChatMessageAsync(chatId: message.Chat.Id, user.messageId);
                await Bot.PinChatMessageAsync(chatId: message.Chat.Id, botMessage.MessageId);
                user.messageId = botMessage.MessageId;
            }
            else
            {

                user.setValueCheck = false;
                await Bot.SendTextMessageAsync(message.Chat.Id, "Не удалось записать значение", replyMarkup: new ReplyKeyboardRemove());
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
        static async Task<Message> NextMigration(Message message, CancellationToken cancellationToken)
        {
            user.migration++;
            UserDataManager.TUsers.Find(n => n.Id == tmpChatId).migration=user.migration;          
            await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                                  text: $"Значение миграции изменено",
                                                                  replyMarkup: new ReplyKeyboardRemove());
            var botMessage = await Bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                       text: $"Последняя миграция - {user.migration}",
                                                       replyMarkup: new ReplyKeyboardRemove());
            if(user.messageId != 0)
                await Bot.UnpinChatMessageAsync(chatId: message.Chat.Id, user.messageId);
            await Bot.PinChatMessageAsync(chatId: message.Chat.Id, botMessage.MessageId);
            user.messageId = botMessage.MessageId;
            await UserDataManager.SaveChanges(tmpChatId, user, logger, cancellationToken);
            return botMessage;
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
    }
}
