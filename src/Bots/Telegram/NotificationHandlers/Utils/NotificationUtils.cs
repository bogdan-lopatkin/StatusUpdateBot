using System;
using System.Globalization;
using EnumStringValues;
using StatusUpdateBot.SpreadSheets;

namespace StatusUpdateBot.Bots.Telegram.NotificationHandlers.Utils
{
    public static class NotificationUtils
    {
        public static bool IsDateValid(object date, out DateTime parsedDate, string dateFormat = "dd/MM/yyyy HH:mm")
        {
            if (date == null)
            {
                parsedDate = default;
                return false;
            }

            if (dateFormat != DateCellFormats.OaDate.GetStringValue())
                return DateTime.TryParseExact(
                    date.ToString(),
                    dateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedDate
                );

            if (!Double.TryParse(date.ToString(), out var oaSerializedDate))
            {
                parsedDate = default;
                return false;
            }

            parsedDate = DateTime.FromOADate(oaSerializedDate);
            
            return true;
        }

        public static bool IsDateObsolete(object date, string dateFormat = "dd/MM/yyyy HH:mm")
        {
            return !IsDateValid(date, out var parsedDate, dateFormat) || parsedDate <= DateTime.Now;
        }
    }
}