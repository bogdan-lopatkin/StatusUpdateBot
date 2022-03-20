﻿using System;
using System.Globalization;

namespace StatusUpdateBot.Bots.Telegram.NotificationHandlers.Utils
{
    public static class NotificationUtils
    {
        public static bool IsDateValid(string date, out DateTime parsedDate, string dateFormat = "dd/MM/yyyy HH:mm")
        {
            return DateTime.TryParseExact(
                date,
                dateFormat,
                null, //CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out parsedDate
            );
        }

        public static bool IsDateObsolete(string date, string dateFormat = "dd/MM/yyyy HH:mm")
        {
            return !IsDateValid(date, out var parsedDate, dateFormat) || parsedDate <= DateTime.Now;
        }
    }
}