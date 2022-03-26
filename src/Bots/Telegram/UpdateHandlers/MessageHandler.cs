using System;
using System.Collections.Generic;
using System.Linq;
using EnumStringValues;
using NickBuhro.Translit;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers;
using StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static StatusUpdateBot.Translators.Translator;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers
{
    public class MessageHandler : IUpdateHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly ISpreadSheet _spreadSheet;
        private IList<object> _userPreferences;

        public MessageHandler(TelegramBotClient botClient, ISpreadSheet spreadSheet)
        {
            _botClient = botClient;
            _spreadSheet = spreadSheet;
        }

        public bool IsApplicable(Update update)
        {
            return update.Message?.Text != null
                   && update.Type == UpdateType.Message
                   && update.Message.Type == MessageType.Text
                   && !update.Message.Text.StartsWith("/");
        }

        public void HandleUpdate(Update update)
        {
            if (update?.Message?.Text == null)
                return;

            FindUserPreferences(update.Message.From);
            if (SpreadSheetUtils.IsRowHasCell(_userPreferences, UserPreferencesSheetCells.Language, out var language))
                SetDefaultTargetLanguage(language);
            
            if (update.Message is {Text: "/changenotificationmode"})
            {
                UpdateHandlerUtils.SendNotificationsPreferenceInlineKeyboardMessage(
                    _botClient,
                    update.Message.Chat.Id,
                    Translate("SelectNotificationMode"),
                    SpreadSheetUtils.TryGetCell(_userPreferences, UserPreferencesSheetCells.NotificationMode)
                );

                return;
            }

            var isNewStatusProvided = update.Message.Text != null && update.Message.Text.Contains("#status");

            _spreadSheet.EnableBatchUpdate();
            _spreadSheet.LoadCache(new[]
                {
                    Sheets.Settings.GetStringValue(),
                }
            );

            bool updateSuccessful;
            try
            {
                var statusData = CreateStatusDataFromUser(update.Message.From);

                if (isNewStatusProvided)
                    ExpandStatusDataWithNewStatus(statusData, update.Message.Text);

                UpdateUserStatus(statusData, update.Message.From);
                UpdateUserChat(update.Message.From, update.Message.Chat);

                updateSuccessful = true;
            }
            catch (Exception e)
            {
                TelegramErrorLogger.LogMessageHandlerError(e, update);
                updateSuccessful = false;
            }

            try
            {
                _spreadSheet.ExecuteBatchUpdate();
            }
            catch (Exception e)
            {
                TelegramErrorLogger.LogError(e, "Произошла ошибка во время записи данных");
            }

            if (isNewStatusProvided)
                AcknowledgeStatusUpdate(update.Message.Chat, update.Message.From, updateSuccessful);
        }

        private Dictionary<int, object> CreateStatusDataFromUser(User user)
        {
            return new Dictionary<int, object>
            {
                {(int) UserStatusSheetCells.Id, user.Id.ToString()},
                {(int) UserStatusSheetCells.Username, user.Username},
                {(int) UserStatusSheetCells.Name, Transliteration.CyrillicToLatin(user.FirstName + " " + user.LastName)},
                {(int) UserStatusSheetCells.LastActivity, DateTime.Now.ToOADate()}
            };
        }

        private void ExpandStatusDataWithNewStatus(IDictionary<int, object> userStatusData, string text)
        {
            userStatusData[(int) UserStatusSheetCells.LastStatusUpdate] = DateTime.Now.ToOADate();
            userStatusData[(int) UserStatusSheetCells.Comment] = text;

            if (Program.Translator != null)
                text = Program.Translator.Translate(text, "en");

            var fixedUserStatusCellsCount = Enum.GetNames(typeof(UserStatusSheetCells)).Length - 1;
            foreach (var entry in MessageHandlerUtils.StatusMessageToArray(text))
                userStatusData[fixedUserStatusCellsCount + entry.Key] = entry.Value;
        }

        private void UpdateUserStatus(Dictionary<int, object> userStatusData, User user)
        {
            _spreadSheet.UpdateOrCreateRow(
                Sheets.Status.GetStringValue(),
                user.Id.ToString(),
                (int) UserStatusSheetCells.Id,
                userStatusData
            );
        }

        private void UpdateUserChat(User user, Chat chat)
        {
            var targetCell = (int) (chat.Type == ChatType.Private
                    ? UserPreferencesSheetCells.ChatId
                    : UserPreferencesSheetCells.GroupId
                );

            if (_userPreferences != null && _userPreferences.Count > targetCell &&
                _userPreferences[targetCell].ToString() != "")
                return;

            _spreadSheet.UpdateOrCreateRow(
                Sheets.Preferences.GetStringValue(),
                user.Id.ToString(),
                (int) UserPreferencesSheetCells.Id,
                new Dictionary<int, object>
                {
                    {(int) UserPreferencesSheetCells.Id, user.Id.ToString()},
                    {targetCell, chat.Id.ToString()}
                }
            );
        }

        private void AcknowledgeStatusUpdate(Chat chat, User user, bool isUpdateSuccessful = true)
        {
            if (chat.Type == ChatType.Private)
                AcknowledgeStatusUpdateInPrivateChat(chat, user, isUpdateSuccessful);

            if (SpreadSheetUtils.IsRowHasCell(_userPreferences, UserPreferencesSheetCells.GroupId))
                AcknowledgeStatusUpdateInGroup(user, isUpdateSuccessful);
        }

        private void AcknowledgeStatusUpdateInPrivateChat(Chat chat, User user, bool isUpdateSuccessful)
        {
            var userHasPreferredNotificationMode = IsUserHasPreferredNotificationMode(user);
            
            if (!int.TryParse(SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NotifyAfter),
                    out var interval))
                interval = 3;

            _botClient.SendTextMessageAsync(chat.Id,
                isUpdateSuccessful
                    ? new StringFormatter(Translate("StatusUpdateSuccess"))
                        .Add("@date",DateTime.Now.AddDays(interval).ToString(DateCellFormats.Date.GetStringValue()))
                        .ToString()
                    : Translate("StatusUpdateError")
            );

            if (!userHasPreferredNotificationMode)
                UpdateHandlerUtils.SendNotificationsPreferenceInlineKeyboardMessage(
                    _botClient,
                    chat.Id,
                    Translate("NotificationModeNotSelected")
                );
        }

        private void AcknowledgeStatusUpdateInGroup(User user, bool isUpdateSuccessful)
        {
            var messageText = SpreadSheetUtils.GetSetting(_spreadSheet, Settings.LastPinnedMessageText);
            var messageId = SpreadSheetUtils.GetSetting(_spreadSheet, Settings.LastPinnedMessageId);

            if (messageText == null || messageId == null)
                return;

            var arr = messageText.Split(Environment.NewLine)
                .Select(s =>
                {
                    var userReference = user.Username != null ? $"@{user.Username}" : $"{user.FirstName} {user.LastName}";

                    return s.Contains(userReference) ? userReference + (isUpdateSuccessful ? " ✅" : " ❌") : s;
                })
                .ToArray();

            try
            {
                if (string.Join(Environment.NewLine, arr) == messageText) return;

                var updatedMessage = _botClient.EditMessageTextAsync(
                    int.Parse(_userPreferences[(int) UserPreferencesSheetCells.GroupId].ToString()!),
                    int.Parse(messageId),
                    string.Join(Environment.NewLine, arr)
                );
                SpreadSheetUtils.SetSetting(_spreadSheet, Settings.LastPinnedMessageText, updatedMessage.Result.Text);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private bool IsUserHasPreferredNotificationMode(User user)
        {
            var (rowId, row) = _spreadSheet.FindRow(
                Sheets.Preferences.GetStringValue(),
                user.Id.ToString()
            );

            return rowId != -1
                   && row.Count > (int) UserPreferencesSheetCells.NotificationMode
                   && row[(int) UserPreferencesSheetCells.NotificationMode].ToString()!.Length > 0;
        }

        private void FindUserPreferences(User user)
        {
            var (rowId, userPreferences) = _spreadSheet.FindRow(
                Sheets.Preferences.GetStringValue(),
                user.Id.ToString()
            );

            if (rowId != 1)
                _userPreferences = userPreferences;
        }
    }
}