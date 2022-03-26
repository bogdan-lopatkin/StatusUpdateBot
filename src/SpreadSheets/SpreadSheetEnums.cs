using EnumStringValues;

namespace StatusUpdateBot.SpreadSheets
{
    public enum Sheets
    {
        Status,
        Preferences,
        Settings,
        Translations
    }

    public enum UserStatusSheetCells
    {
        Id,
        Username,
        Name,
        LastActivity,
        LastStatusUpdate,
        Comment
    }

    public enum UserPreferencesSheetCells
    {
        Id,
        ChatId,
        GroupId,
        NotificationMode,
        Language
    }

    public enum SettingsSheetCells
    {
        Id,
        Description,
        Value
    }

    public enum DateCellFormats
    {
        [StringValue("dd/MM/yyyy")]
        Date,
        
        [StringValue("dd/MM/yyyy HH:mm")]
        DateTime,
        
        OaDate,
    }
    
    public enum TranslationsSheetCells
    {
        Key,
        Ua,
        Ru,
    }
}