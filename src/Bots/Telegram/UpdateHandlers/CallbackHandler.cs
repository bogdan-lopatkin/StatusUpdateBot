using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnumStringValues;
using StatusUpdateBot.SpreadSheets;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers
{
    public class CallbackHandler : IUpdateHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly ISpreadSheet _spreadSheet;

        public CallbackHandler(TelegramBotClient botClient, ISpreadSheet spreadSheet)
        {
            _botClient = botClient;
            _spreadSheet = spreadSheet;
        }

        public bool IsApplicable(Update update)
        {
            return update.Type == UpdateType.CallbackQuery;
        }

        public void HandleUpdate(Update update)
        {
            if (update.CallbackQuery?.Data == null)
                return;

            try
            {
                var callBackDataParts = update.CallbackQuery.Data.Split(".");

                if (callBackDataParts.Select(s => s.Length > 0).Count() != 3)
                    return;

                var updateProcessingMethod = (CallBackHandlerUpdateProcessingMethods) Enum.Parse(
                    typeof(CallBackHandlerUpdateProcessingMethods),
                    callBackDataParts[(int) CallbackStructure.Type],
                    true
                );

                var callbackRelatedMethod = GetType().GetMethod(
                    updateProcessingMethod.GetStringValue(),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] {typeof(Update)},
                    null
                );

                if (callbackRelatedMethod != null)
                    callbackRelatedMethod.Invoke(this, new object[] {update});
            }
            catch (Exception e)
            {
                TelegramErrorLogger.LogCallbackHandlerError(e, update);
            }
        }

        protected void UpdateNotificationPreference(Update update)
        {
            var type = update.CallbackQuery.Data.Split(".")[(int) CallbackStructure.Value];

            var notificationPreference = (NotificationPreferences) Enum.Parse(
                typeof(NotificationPreferences),
                type,
                true
            );

            _spreadSheet.FindAndUpdateRow(
                Sheets.Preferences.GetStringValue(),
                update.CallbackQuery.From.Id.ToString(),
                (int) UserPreferencesSheetCells.Id,
                new Dictionary<int, string>
                {
                    {(int) UserPreferencesSheetCells.NotificationMode, type}
                }
            );

            _botClient.SendTextMessageAsync(
                update.CallbackQuery.Message.Chat.Id,
                notificationPreference.GetStringValue()
            );
        }
    }

    internal enum CallbackStructure
    {
        Prefix,
        Type,
        Value
    }

    internal enum CallBackHandlerUpdateProcessingMethods
    {
        [StringValue("UpdateNotificationPreference")]
        NotificationPreference
    }
}