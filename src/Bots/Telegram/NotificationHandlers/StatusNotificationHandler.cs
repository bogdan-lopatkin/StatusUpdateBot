using System;
using System.Collections.Generic;
using System.Linq;
using EnumStringValues;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers.Utils;
using StatusUpdateBot.SpreadSheets;
using Telegram.Bot;

namespace StatusUpdateBot.Bots.Telegram.NotificationHandlers
{
    public class StatusNotificationHandler : INotificationHandler
    {
        private const string DefaultUserNotificationText = "Пожалуйста, обновите статус";
        private readonly TelegramBotClient _botClient;

        private readonly ISpreadSheet _spreadSheet;
        private int _notificationInterval;

        public StatusNotificationHandler(TelegramBotClient botClient, ISpreadSheet spreadSheet)
        {
            _spreadSheet = spreadSheet;
            _botClient = botClient;
        }

        public void SendNotifications()
        {
            _spreadSheet.LoadCache(new[] {Sheets.Settings.GetStringValue()});
            _spreadSheet.EnableBatchUpdate();

            var updateAt = SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NextNotificationAt)
                           + " "
                           + SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NotifyAt);

            if (!NotificationUtils.IsDateObsolete(updateAt))
                return;

            _spreadSheet.LoadCache(
                new[]
                {
                    Sheets.Status.GetStringValue(),
                    Sheets.Preferences.GetStringValue()
                }
            );

            _notificationInterval
                = int.Parse(SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NotifyAfter, "3"));

            var usersWithOverdueStatus = GetAllUsersWithOverdueStatus();
            var groupedByPreferenceUsers = GroupUserStatusesByUserNotificationPreference(usersWithOverdueStatus);

            NotifyUsersInGroup(groupedByPreferenceUsers[(int) NotificationPreferences.Group]);
            NotifyUsersInPrivateChat(groupedByPreferenceUsers[(int) NotificationPreferences.Private]);

            SpreadSheetUtils.SetSetting(
                _spreadSheet,
                Settings.NextNotificationAt,
                DateTime.Now.AddDays(_notificationInterval).ToString("dd/M/yyyy")
            );

            _spreadSheet.ExecuteBatchUpdate();
        }

        private IList<IList<object>> GetAllUsersWithOverdueStatus()
        {
            var userStatuses = _spreadSheet.GetAllRows(Sheets.Status.GetStringValue());

            return userStatuses.Where(IsStatusShouldBeUpdated).ToList();
        }

        private bool IsStatusShouldBeUpdated(IList<object> userStatus)
        {
            return !NotificationUtils.IsDateValid(
                userStatus[(int) UserStatusSheetCells.LastStatusUpdate].ToString(),
                out var parsedDate,
                "dd/M/yyyy"
            ) || parsedDate.AddDays(_notificationInterval) < DateTime.Now;
        }

        private Dictionary<int, IList<UserData>> GroupUserStatusesByUserNotificationPreference(
            IList<IList<object>> userStatuses)
        {
            Dictionary<int, IList<UserData>> groupedUserStatuses = new()
            {
                {(int) NotificationPreferences.Group, new List<UserData>()},
                {(int) NotificationPreferences.Private, new List<UserData>()}
            };

            foreach (var userStatus in userStatuses)
            {
                var (_, userPreferences) = _spreadSheet.FindRow(
                    Sheets.Preferences.GetStringValue(),
                    userStatus[(int) UserStatusSheetCells.Id].ToString()
                );

                var notificationPreference
                    = SpreadSheetUtils.IsRowHasCell(userPreferences, UserPreferencesSheetCells.GroupId)
                        ? NotificationPreferences.Group
                        : NotificationPreferences.None;

                if (SpreadSheetUtils.IsRowHasCell(userPreferences, UserPreferencesSheetCells.NotificationMode))
                    notificationPreference = (NotificationPreferences) Enum.Parse(
                        typeof(NotificationPreferences),
                        userPreferences[(int) UserPreferencesSheetCells.NotificationMode].ToString()!,
                        true
                    );

                if (notificationPreference == NotificationPreferences.None)
                    continue;

                groupedUserStatuses[(int) notificationPreference].Add(new UserData(userStatus, userPreferences));
            }

            return groupedUserStatuses;
        }

        private void NotifyUsersInGroup(IList<UserData> usersData)
        {
            var usersDataGroupedByGroupId = GroupUserDataByUserGroupId(usersData);

            foreach (var entry in usersDataGroupedByGroupId)
            {
                var message = SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NotificationText,
                    DefaultUserNotificationText);
                var userList = CreateUserList(entry.Value);

                var sentMessage = _botClient.SendTextMessageAsync(
                    entry.Key,
                    message + userList,
                    disableNotification: true
                );

                try
                {
                    _botClient.UnpinChatMessageAsync(
                        entry.Key,
                        int.Parse(SpreadSheetUtils.GetSetting(_spreadSheet, Settings.LastPinnedMessageId))
                    );
                }
                catch (Exception)
                {
                }

                _botClient.PinChatMessageAsync(entry.Key, sentMessage.Result.MessageId);
                SpreadSheetUtils.SetSetting(_spreadSheet, Settings.LastPinnedMessageText, sentMessage.Result.Text);
                SpreadSheetUtils.SetSetting(_spreadSheet, Settings.LastPinnedMessageId,
                    sentMessage.Result.MessageId.ToString());
            }
        }

        private void NotifyUsersInPrivateChat(IList<UserData> usersData)
        {
            foreach (var userData in usersData)
            {
                if (userData.UserPreferences[(int) UserPreferencesSheetCells.ChatId].ToString() == "")
                    continue;

                _botClient.SendTextMessageAsync(
                    userData.UserPreferences[(int) UserPreferencesSheetCells.ChatId].ToString()!,
                    SpreadSheetUtils.GetSetting(_spreadSheet, Settings.NotificationText, DefaultUserNotificationText)
                );
            }
        }

        private Dictionary<string, IList<UserData>> GroupUserDataByUserGroupId(IList<UserData> usersData)
        {
            Dictionary<string, IList<UserData>> result = new();

            foreach (var userData in usersData)
            {
                var groupId = userData.UserPreferences[(int) UserPreferencesSheetCells.GroupId].ToString()!;
                if (!result.ContainsKey(groupId))
                    result[groupId] = new List<UserData>();

                result[groupId].Add(userData);
            }

            return result;
        }

        private string CreateUserList(IList<UserData> usersData)
        {
            var resultList = $"{Environment.NewLine + Environment.NewLine}";

            foreach (var userData in usersData)
            {
                var userName = userData.UserStatus[(int) UserStatusSheetCells.Username].ToString();
                var name = userData.UserStatus[(int) UserStatusSheetCells.Name].ToString();

                resultList += (userName != "" ? $"@{userName}" : $" {name}") + Environment.NewLine;
            }

            return resultList;
        }

        private struct UserData
        {
            public readonly IList<object> UserStatus;
            public readonly IList<object> UserPreferences;

            public UserData(IList<object> userStatus, IList<object> userPreferences)
            {
                UserStatus = userStatus;
                UserPreferences = userPreferences;
            }
        }
    }
}