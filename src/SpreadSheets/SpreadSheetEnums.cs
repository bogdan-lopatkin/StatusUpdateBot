﻿using EnumStringValues;

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
        [StringValue("dd/M/yyyy")]
        Date,
        
        [StringValue("dd/M/yyyy HH:mm")]
        DateTime
    }
    
    public enum TranslationsSheetCells
    {
        Key,
        Ua,
        Ru,
    }
}