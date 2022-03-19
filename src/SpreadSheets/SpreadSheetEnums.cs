namespace StatusUpdateBot.SpreadSheets
{
    public enum Sheets
    {
        Status,
        Preferences,
        Settings
    }

    public enum UserStatusSheetCells
    {
        Id,
        Username,
        Name,
        LastActivity,
        LastStatusUpdate
    }

    public enum UserPreferencesSheetCells
    {
        Id,
        ChatId,
        GroupId,
        NotificationMode
    }

    public enum SettingsSheetCells
    {
        Id,
        Description,
        Value
    }
}