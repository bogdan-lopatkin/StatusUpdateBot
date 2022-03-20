using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using StatusUpdateBot.Bots;
using StatusUpdateBot.Bots.Telegram;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.SpreadSheets.Google;
using StatusUpdateBot.Translators.External;

namespace StatusUpdateBot
{
    internal static class Program
    {
        public static IConfiguration Configuration;
        public static IExternalTranslator Translator;

        private static void Main()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            Translator = new MicrosoftTranslator(
                Configuration.GetSection("Translator:Microsoft:Endpoint").Value,
                Configuration.GetSection("Translator:Microsoft:ApiKey").Value,
                Configuration.GetSection("Translator:Microsoft:Region").Value
            );
            
            var spreadSheet = 
                new GoogleSpreadSheets(Configuration.GetSection("SpreadSheet:Google:SpreadSheetId").Value);

            Translators.Translator.LoadValuesFromSpreadSheet(spreadSheet);
            Translators.Translator.SetDefaultTargetLanguage(SpreadSheetUtils.GetSetting(spreadSheet, Settings.LanguageInGroup));

            IBot bot = new TelegramBot(
                Configuration.GetSection("Bot:Telegram:Token").Value,
                spreadSheet
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