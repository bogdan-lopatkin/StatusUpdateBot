using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnumStringValues;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers;
using StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.Translators;
using StatusUpdateBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
            var type = update.CallbackQuery!.Data!.Split(".")[(int) CallbackStructure.Value];

            var notificationPreference = (NotificationPreferences) Enum.Parse(
                typeof(NotificationPreferences),
                type,
                true
            );

            _spreadSheet.FindAndUpdateRow(
                Sheets.Preferences.GetStringValue(),
                update.CallbackQuery.From.Id.ToString(),
                (int) UserPreferencesSheetCells.Id,
                new Dictionary<int, object>
                {
                    {(int) UserPreferencesSheetCells.NotificationMode, type}
                }
            );

            UpdateHandlerUtils.MarkSelectedNotificationModeButton(update.CallbackQuery!.Message!.ReplyMarkup, type);

            _botClient.EditMessageTextAsync(
                update.CallbackQuery.Message.Chat.Id,
                update.CallbackQuery.Message.MessageId,
                new StringFormatter(Translator.Translate("SelectNotificationMode"))
                    .Add("@currentMode", Translator.Translate(notificationPreference.GetStringValue()))
                    .ToString(),
                replyMarkup: update.CallbackQuery.Message.ReplyMarkup
            );
        }
        
        protected void UpdateLanguagePreference(Update update)
        {
            var language = update.CallbackQuery!.Data!.Split(".")[(int) CallbackStructure.Value];

            _spreadSheet.FindAndUpdateRow(
                Sheets.Preferences.GetStringValue(),
                update.CallbackQuery.From.Id.ToString(),
                (int) UserPreferencesSheetCells.Id,
                new Dictionary<int, object>
                {
                    {(int) UserPreferencesSheetCells.Language, language}
                }
            );

            var keyboardRows = update.CallbackQuery!.Message!.ReplyMarkup!.InlineKeyboard;

            foreach (var keyboardRow in keyboardRows)
                foreach (var button in keyboardRow)
                {
                    button.Text = button.Text.Replace(" ✅", null);
                    
                    if (button.Text.Contains(language))
                        button.Text += " ✅";
                }   
            
            _botClient.EditMessageTextAsync(
                update.CallbackQuery.Message.Chat.Id,
                update.CallbackQuery.Message.MessageId,
                Translator.Translate("SelectLanguage", language),
                replyMarkup: new InlineKeyboardMarkup(keyboardRows)
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
        NotificationPreference,
        [StringValue("UpdateLanguagePreference")]
        Language
    }
}