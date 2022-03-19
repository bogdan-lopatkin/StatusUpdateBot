using System;
using System.Collections.Generic;
using System.Linq;
using EnumStringValues;
using StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils;
using StatusUpdateBot.SpreadSheets;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers
{
    public class MessageHandler : IUpdateHandler
    {
        private const string LastActivityDateTimeFormat = "dd/M/yyyy HH:mm";
        private const string LastStatusUpdateDateTimeFormat = "dd/M/yyyy";

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
            return update.Message != null && update.Type == UpdateType.Message &&
                   update.Message.Type == MessageType.Text;
        }

        public void HandleUpdate(Update update)
        {
            if (update?.Message?.Text == null)
                return;

            if (update.Message is {Text: "Изменить режим получения напоминаний"})
            {
                SendNotificationsPreferenceInlineKeyboardMessage(
                    update.Message.Chat.Id,
                    "Выберите новый режим получения напоминаний"
                );

                return;
            }

            var isNewStatusProvided = update.Message.Text != null && update.Message.Text.Contains("#status");

            _spreadSheet.EnableBatchUpdate();
            _spreadSheet.LoadCache(new[]
                {
                    Sheets.Settings.GetStringValue(),
                    Sheets.Preferences.GetStringValue()
                }
            );

            bool updateSuccessful;
            try
            {
                var statusData = CreateStatusDataFromUser(update.Message.From);

                if (isNewStatusProvided)
                    ExpandStatusDataWithNewStatus(statusData, update.Message.Text);

                UpdateUserStatus(statusData, update.Message.From);

                FindUserPreferences(update.Message.From);
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

        private Dictionary<int, string> CreateStatusDataFromUser(User user)
        {
            return new Dictionary<int, string>
            {
                {(int) UserStatusSheetCells.Id, user.Id.ToString()},
                {(int) UserStatusSheetCells.Username, user.Username},
                {(int) UserStatusSheetCells.Name, user.FirstName + " " + user.LastName},
                {(int) UserStatusSheetCells.LastActivity, DateTime.Now.ToString(LastActivityDateTimeFormat)}
            };
        }

        private void ExpandStatusDataWithNewStatus(IDictionary<int, string> userStatusData, string text)
        {
            userStatusData[(int) UserStatusSheetCells.LastStatusUpdate] =
                DateTime.Now.ToString(LastStatusUpdateDateTimeFormat);

            var fixedUserStatusCellsCount = Enum.GetNames(typeof(UserStatusSheetCells)).Length - 1;
            foreach (var entry in MessageHandlerUtils.StatusMessageToArray(text))
                userStatusData[fixedUserStatusCellsCount + entry.Key] = entry.Value;
        }

        private void UpdateUserStatus(Dictionary<int, string> userStatusData, User user)
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
                new Dictionary<int, string>
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
                AcknowledgeStatusUpdateInGroup(chat, user, isUpdateSuccessful);
        }

        private void AcknowledgeStatusUpdateInPrivateChat(Chat chat, User user, bool isUpdateSuccessful)
        {
            var userHasPreferredNotificationMode = IsUserHasPreferredNotificationMode(user);

            ReplyKeyboardMarkup replyKeyboardMarkup =
                new(new[] {new KeyboardButton[] {"Изменить режим получения напоминаний"}})
                {
                    ResizeKeyboard = true
                };

            if (!int.TryParse(SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NextNotificationAt),
                    out var interval))
                interval = 3;

            _botClient.SendTextMessageAsync(chat.Id,
                isUpdateSuccessful
                    ? $"Статус был обновлен✅. Следующее обновление статуса - {DateTime.Now.AddDays(interval):dd/M/yyyy}."
                    : "Произошла ошибка, обновленный статус будет обработан вручную позже",
                replyMarkup: userHasPreferredNotificationMode ? replyKeyboardMarkup : null
            );

            if (!userHasPreferredNotificationMode)
                SendNotificationsPreferenceInlineKeyboardMessage(
                    chat.Id,
                    "Похоже у Вас не выбран предпочтительный режим получения напоминаний. Пожалуйста, выберите его сейчас. По умолчанию напоминания приходят в группу."
                );
        }

        private void AcknowledgeStatusUpdateInGroup(Chat chat, User user, bool isUpdateSuccessful)
        {
            var messageText = SpreadSheetUtils.GetSetting(_spreadSheet, Settings.LastPinnedMessageText);
            var messageId = SpreadSheetUtils.GetSetting(_spreadSheet, Settings.LastPinnedMessageId);

            if (messageText == null || messageId == null)
                return;

            var arr = messageText.Split(Environment.NewLine)
                .Select(s =>
                {
                    var userReference = user.Username ?? $"{user.FirstName} {user.LastName}";

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

        private void SendNotificationsPreferenceInlineKeyboardMessage(long chatId, string text)
        {
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Получать напоминания в ЛС",
                            "callback.notificationPreference.private")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Получать напоминания в группе",
                            "callback.notificationPreference.group")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Не получать напоминания",
                            "callback.notificationPreference.none")
                    }
                }
            );

            _botClient.SendTextMessageAsync(
                chatId,
                text,
                replyMarkup: inlineKeyboard
            );
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