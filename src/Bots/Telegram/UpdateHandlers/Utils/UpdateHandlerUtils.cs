using System;
using System.Collections.Generic;
using System.Linq;
using EnumStringValues;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static StatusUpdateBot.Translators.Translator;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils
{
    public static class UpdateHandlerUtils
    {
        public static void SendNotificationsPreferenceInlineKeyboardMessage(TelegramBotClient botClient, long chatId,
            string text, string currentMode = null)
        {
            string notificationMode = null;
            
            try
            {
                notificationMode = currentMode != null ? ((NotificationPreferences) Enum.Parse(
                    typeof(NotificationPreferences),
                    currentMode,
                    true
                )).GetStringValue() : NotificationPreferences.NotSet.GetStringValue();
            }
            catch (Exception) { }
            
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Translate("SelectNotificationModePrivate"),
                            "callback.notificationPreference.Private")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Translate("SelectNotificationModeGroup"),
                            "callback.notificationPreference.Group")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(Translate("SelectNotificationModeNone"),
                            "callback.notificationPreference.None")
                    }
                }
            );
            
            MarkSelectedNotificationModeButton(inlineKeyboard, currentMode);

            botClient.SendTextMessageAsync(
                chatId,
                new StringFormatter(text)
                    .Add("@currentMode", Translate(notificationMode))
                    .ToString(),
                replyMarkup: inlineKeyboard
            );
        }

        public static void MarkSelectedNotificationModeButton(InlineKeyboardMarkup keyboard, string currentMode)
        {
            if (String.IsNullOrEmpty(currentMode))
                return;
            
            foreach (var keyboardRow in keyboard.InlineKeyboard)
            {
                var button = keyboardRow.First();

                button.Text = button.Text.Replace(" ✅", null);

                if (button.CallbackData!.Contains(currentMode))
                    button.Text += " ✅";
            }
        }

        public static IList<object> FindUserPreferences(ISpreadSheet spreadSheet, User user)
        {
            var (_, userPreferences) = spreadSheet.FindRow(
                Sheets.Preferences.GetStringValue(),
                user.Id.ToString()
            );

            return userPreferences;
        }
    }
}