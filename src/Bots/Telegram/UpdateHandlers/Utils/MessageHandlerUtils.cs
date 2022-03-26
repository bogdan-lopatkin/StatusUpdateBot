using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers.Utils
{
    public static class MessageHandlerUtils
    {
        public static Dictionary<int, string> StatusMessageToArray(string message)
        {
            var result = Regex.Split(message, @"(\d+[\.\ ]+)+").Where(s => s.Length > 0).ToArray();

            Dictionary<int, string> values = new();
            for (var i = 1; i < result.Length; i += 2)
            {
                var numeration = int.Parse(Regex.Match(result[i], @"\d+").Value);
                values[numeration] = result[i + 1].Trim();
            }

            return values;
        }
    }
}