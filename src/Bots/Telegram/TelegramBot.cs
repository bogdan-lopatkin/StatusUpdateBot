using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers;
using StatusUpdateBot.SpreadSheets;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using IUpdateHandler = StatusUpdateBot.Bots.Telegram.UpdateHandlers.IUpdateHandler;

namespace StatusUpdateBot.Bots.Telegram
{
    public class TelegramBot : IBot
    {
        private readonly TelegramBotClient _botClient;
        private readonly CancellationTokenSource _cts;
        private readonly List<INotificationHandler> _notificationHandlers;
        private readonly ISpreadSheet _spreadSheet;
        private readonly List<IUpdateHandler> _updateHandlers;
        private Timer _timer;

        public TelegramBot(string token, ISpreadSheet spreadSheet, List<IUpdateHandler> updateHandlers = null,
            List<INotificationHandler> notificationHandlers = null)
        {
            _botClient = new TelegramBotClient(token);
            _cts = new CancellationTokenSource();
            _spreadSheet = spreadSheet;

            _updateHandlers = updateHandlers ?? GetAllUpdateHandlers();
            _notificationHandlers = notificationHandlers ?? GetAllNotificationHandlers();
        }

        public void StartReceivingMessages()
        {
            var receiverOptions = new ReceiverOptions();

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token);
        }

        public void StartNotifyingUsers()
        {
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);

            _timer = new Timer(_ =>
            {
                foreach (var handler in _notificationHandlers) handler.SendNotifications();
            }, null, startTimeSpan, periodTimeSpan);
        }

        public void StartLogging()
        {
            TelegramErrorLogger.SetBotClient(_botClient);
        }

        private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            foreach (var handler in _updateHandlers.Where(handler => handler.IsApplicable(update)))
            {
                handler.HandleUpdate(update);
                break;
            }

            return Task.CompletedTask;
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(DateTime.Now + " - " + errorMessage);

            return Task.CompletedTask;
        }

        private List<IUpdateHandler> GetAllUpdateHandlers()
        {
            var interfaceType = typeof(IUpdateHandler);

            var updateHandlerClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(interfaceType.IsAssignableFrom).ToList();

            updateHandlerClasses.Remove(interfaceType);

            return updateHandlerClasses.Select(updateHandlerClass =>
                    (IUpdateHandler) Activator.CreateInstance(updateHandlerClass, _botClient, _spreadSheet))
                .ToList();
        }

        private List<INotificationHandler> GetAllNotificationHandlers()
        {
            var interfaceType = typeof(INotificationHandler);

            var updateHandlerClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(interfaceType.IsAssignableFrom).ToList();

            updateHandlerClasses.Remove(interfaceType);

            return updateHandlerClasses.Select(notificationHandlerClass =>
                    (INotificationHandler) Activator.CreateInstance(notificationHandlerClass, _botClient, _spreadSheet.Clone()))
                .ToList();
        }
    }
}