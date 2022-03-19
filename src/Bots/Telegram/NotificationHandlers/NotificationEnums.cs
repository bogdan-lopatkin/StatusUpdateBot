using EnumStringValues;

namespace StatusUpdateBot.Bots.Telegram
{
    public enum NotificationPreferences
    {
        [StringValue("Готово! Напоминания об обновлении статуса отключены")]
        None,

        [StringValue("Готово! Последующие напоминания об обновлении статуса будут приходить в группу")]
        Group,

        [StringValue("Готово! Последующие напоминания об обновлении статуса будут приходить в ЛС")]
        Private
    }

    public enum Settings
    {
        NotifyAfter,
        NotifyAt,
        NextNotificationAt,
        NotificationText,
        LastPinnedMessageText,
        LastPinnedMessageId
    }
}