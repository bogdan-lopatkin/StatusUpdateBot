using EnumStringValues;

namespace StatusUpdateBot.Bots.Telegram.NotificationHandlers
{
    public enum NotificationPreferences
    {
        [StringValue("NotificationModeNone")]
        None,

        [StringValue("NotificationModeGroup")]
        Group,

        [StringValue("NotificationModePrivate")]
        Private,
        
        [StringValue("NotificationModeNotSet")]
        NotSet,
    }

    public enum Settings
    {
        NotifyAfter,
        NotifyAt,
        NextNotificationAt,
        LastPinnedMessageText,
        LastPinnedMessageId,
        LanguageInGroup
    }
}