using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EnumStringValues;
using StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.Translators;
using StatusUpdateBot.Utils;
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
        private User BotInfo;

        public CommandHandler(TelegramBotClient botClient, ISpreadSheet spreadSheet)
        {
            _botClient = botClient;
            _spreadSheet = spreadSheet;
            BotInfo = _botClient.GetMeAsync().Result;
        }

        public bool IsApplicable(Update update)
        {
            // TODO enable after /enroll command is moved to admin-only keyboard
            // bool messageSentToCurrentBot
                // = update?.Message?.Chat.Type != ChatType.Group || (bool)update.Message?.Text?.Contains(BotInfo.Username!);

            return update.Message?.Text != null
                   // && messageSentToCurrentBot
                   && update.Type == UpdateType.Message
                   && update.Message.Type == MessageType.Text
                   && update.Message.Text.StartsWith("/");
        }

        public void HandleUpdate(Update update)
        {
            _userPreferences = UpdateHandlerUtils.FindUserPreferences(_spreadSheet, update.Message!.From);
            _botClient.DeleteMessageAsync(update.Message.Chat.Id, update.Message.MessageId);
            
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

            if (update.Message.Text.Contains("/enroll"))
            {
                EnrollAdmin(update);
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

        private void EnrollAdmin(Update update)
        {
            ChatMember author = _botClient.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id).Result;
            
            if (author.Status is not (ChatMemberStatus.Administrator or ChatMemberStatus.Creator))
                return;

            string message = new StringFormatter(Translate("AdminEnrolled"))
                .Add("@lastName", author.User.LastName)
                .Add("@firstName", author.User.FirstName)
                .Add("@login", "@" + author.User.Username)
                .Add("@enrolledFor", Regex.Match(update.Message.Text, @"\d+").Value)
                .ToString();

            var e =_botClient.SendTextMessageAsync(
                update.Message.Chat.Id,
                message,
                ParseMode.Html
            ).Result;
        }
    }
}