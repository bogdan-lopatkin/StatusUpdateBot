using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using StatusUpdateBot.Bots;
using StatusUpdateBot.Bots.Telegram;
using StatusUpdateBot.SpreadSheets.Google;

namespace StatusUpdateBot
{
    internal static class Program
    {
        public static IConfiguration Configuration;

        private static void Main()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            IBot bot = new TelegramBot(
                Configuration.GetSection("Bot:Telegram:Token").Value,
                new GoogleSpreadSheets(Configuration.GetSection("SpreadSheet:Google:SpreadSheetId").Value)
            );

            bot.StartReceivingMessages();
            bot.StartNotifyingUsers();

            if (bool.Parse(Configuration.GetSection("Bot:Telegram:EnableLogging").Value ?? ""))
                bot.StartLogging();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}