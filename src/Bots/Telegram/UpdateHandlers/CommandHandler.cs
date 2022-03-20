using System;
using System.Collections.Generic;
using EnumStringValues;
using StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.Translators;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static StatusUpdateBot.Translators.Translator;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers
{
    public class CommandHandler : IUpdateHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly ISpreadSheet _spreadSheet;
        private IList<object> _userPreferences;

        public CommandHandler(TelegramBotClient botClient, ISpreadSheet spreadSheet)
        {
            _botClient = botClient;
            _spreadSheet = spreadSheet;
        }

        public bool IsApplicable(Update update)
        {
            return update.Message?.Text != null
                   && update.Type == UpdateType.Message
                   && update.Message.Type == MessageType.Text
                   && update.Message.Text.StartsWith("/");
        }

        public void HandleUpdate(Update update)
        {
            _userPreferences = UpdateHandlerUtils.FindUserPreferences(_spreadSheet, update.Message!.From);
            
            if (SpreadSheetUtils.IsRowHasCell(_userPreferences, UserPreferencesSheetCells.Language, out var language))
                SetDefaultTargetLanguage(language);
            
            if (update.Message is {Text: "/changenotificationmode"})
            {
                UpdateHandlerUtils.SendNotificationsPreferenceInlineKeyboardMessage(
                    _botClient,
                    update.Message.Chat.Id,
                    Translate("SelectNotificationMode"),
                    SpreadSheetUtils.IsRowHasCell(_userPreferences, UserPreferencesSheetCells.NotificationMode, out var mode)
                        ? mode
                        : null
                );
            }
            
            if (update.Message is {Text: "/setlanguage"} or {Text: "/start"})
            {
                SendLanguageSelectionMarkupKeyboardMessage(update.Message.Chat, Translate("SelectLanguage"));
            }
        }

        private void SendLanguageSelectionMarkupKeyboardMessage(Chat chat, string text)
        {
            List<InlineKeyboardButton[]> keyboardButtons = new();
            List<InlineKeyboardButton> buttonRow = new();
            
            foreach (Languages language in Enum.GetValues(typeof(Languages)))
            {
                buttonRow.Add(InlineKeyboardButton.WithCallbackData(
                    language.GetStringValue() + (language.GetStringValue() == GetDefaultTargetLanguage() ? " ✅" : ""),
                        $"callback.language.{language.GetStringValue()}"
                ));

                if (buttonRow.Count != 2)
                    continue;
                
                keyboardButtons.Add(buttonRow.ToArray());
                buttonRow = new();
            }

            if (buttonRow.Count != 0)
                keyboardButtons.Add(buttonRow.ToArray());

            _botClient.SendTextMessageAsync(
                chat.Id,
                text,
                replyMarkup: new InlineKeyboardMarkup(keyboardButtons)
            );
        }
    }
}