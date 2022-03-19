using System;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace StatusUpdateBot.Bots.Telegram
{
    public class TelegramErrorLogger
    {
        private static TelegramBotClient _botClient;

        public static void SetBotClient(TelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        public static void LogMessageHandlerError(Exception e, Update update)
        {
            try
            {
                _botClient.SendTextMessageAsync(
                    Program.Configuration.GetSection("Bot:Telegram:DeveloperChatId").Value,
                    $"Во время обработки статуса произошла критическая ошибка {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Отправитель - {update.Message.From.Id} | {update.Message.From.Username} {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Текст - {update.Message.Text} {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Ошибка - {e.Message} {Environment.NewLine}{Environment.NewLine} {e.StackTrace}"
                );

                Console.WriteLine("Error occured and was reported");
            }
            catch (Exception)
            {
            }
        }

        public static void LogCallbackHandlerError(Exception e, Update update)
        {
            try
            {
                _botClient.SendTextMessageAsync(
                    Program.Configuration.GetSection("Bot:Telegram:DeveloperChatId").Value,
                    $"Во время обработки callback'a произошла критическая ошибка {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Отправитель - {update.CallbackQuery.From.Id} | {update.CallbackQuery.From.Username} {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Callback - {update.CallbackQuery.Data} {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Ошибка - {e.Message} {Environment.NewLine}{Environment.NewLine} {e.StackTrace}"
                );

                Console.WriteLine("Error occured and was reported");
            }
            catch (Exception)
            {
            }
        }

        public static void LogError(Exception e, string description = "")
        {
            try
            {
                _botClient.SendTextMessageAsync(
                    Program.Configuration.GetSection("Bot:Telegram:DeveloperChatId").Value,
                    $"{description} {Environment.NewLine}" +
                    "--------------------------------------------------------------------------" +
                    $"Ошибка - {e.Message} {Environment.NewLine}{Environment.NewLine} {e.StackTrace}"
                );

                Console.WriteLine("Error occured and was reported");
            }
            catch (Exception)
            {
            }
        }
    }
}