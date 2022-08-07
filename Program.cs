using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using StatusUpdateBot.Bots;
using StatusUpdateBot.Bots.Telegram;
using StatusUpdateBot.Bots.Telegram.NotificationHandlers;
using StatusUpdateBot.SpreadSheets;
using StatusUpdateBot.SpreadSheets.Google;
using StatusUpdateBot.SpreadSheets.Local;
using StatusUpdateBot.Translators.External;

namespace StatusUpdateBot
{
    internal static class Program
    {
        public static IConfiguration Configuration;
        public static IExternalTranslator Translator;
        private static Timer _timer;

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
            
            var remoteSpreadSheet = new GoogleSpreadSheets(Configuration.GetSection("SpreadSheet:Google:SpreadSheetId").Value);
            var spreadSheet = new JsonToSpreadSheetAdapter(sourceSheet: remoteSpreadSheet);

            Translators.Translator.LoadValuesFromSpreadSheet(spreadSheet);
            Translators.Translator.SetDefaultTargetLanguage(SpreadSheetUtils.GetSetting(spreadSheet, Settings.LanguageInGroup));

            IBot bot = new TelegramBot(
                Configuration.GetSection("Bot:Telegram:Token").Value,
                spreadSheet
            );

            bot.StartReceivingMessages();
            bot.StartNotifyingUsers();
            StartSyncingSheets(spreadSheet, remoteSpreadSheet);

            if (bool.Parse(Configuration.GetSection("Bot:Telegram:EnableLogging").Value ?? ""))
                bot.StartLogging();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void StartSyncingSheets(ISpreadSheet localSpreadSheet, ISpreadSheet remoteSpreadSheet)
        {
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(5);

            _timer = new Timer(_ =>
            {
                SpreadSheetUtils.SyncSheets(
                    localSpreadSheet,
                    remoteSpreadSheet,
                    new[]
                    {
                        Sheets.Status.ToString(),
                        Sheets.Preferences.ToString(),
                        Sheets.Settings.ToString(),
                    }
                );

                SpreadSheetUtils.SyncSheets(
                    remoteSpreadSheet,
                    localSpreadSheet,
                    new[] { Sheets.Translations.ToString() }
                );
            }, null, startTimeSpan, periodTimeSpan);
        }
    }
}