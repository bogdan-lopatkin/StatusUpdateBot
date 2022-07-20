using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnumStringValues;
using StatusUpdateBot.SpreadSheets;

namespace StatusUpdateBot.Translators
{
    internal static class Translator
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Dictionary = new();
        private static string _defaultTargetLanguage;
        private static Timer _timer;

        public static void SetDefaultTargetLanguage(string language)
        {
            if (String.IsNullOrEmpty(language))
                return;
            
            _defaultTargetLanguage = language;
        }
        
        public static string GetDefaultTargetLanguage()
        {
            return _defaultTargetLanguage;
        }
        
        static Translator()
        {
            _defaultTargetLanguage = Languages.UA.GetStringValue();
        }

        public static string Translate(string key, string to = null)
        {
            if (!Dictionary.TryGetValue(key, out var translations))
                return key;

            return translations.TryGetValue(to ?? _defaultTargetLanguage, out var translation) ? translation : key;
        }

        public static void LoadValuesFromSpreadSheet(ISpreadSheet spreadSheet, Enum fromSheet = null, bool hotReload = true)
        {
            var translationRows = spreadSheet is ICachedSpreadSheet cachedSpreadSheet
                ? cachedSpreadSheet.GetAllRows(Sheets.Translations.GetStringValue(), true)
                : spreadSheet.GetAllRows(Sheets.Translations.GetStringValue());

            if (translationRows == null || translationRows.Count == 0)
                return;
            
            var headers = translationRows.First();
            translationRows.RemoveAt(0);

            foreach (var translationRow in translationRows)
            {
                if (!SpreadSheetUtils.IsRowHasCell(translationRow, TranslationsSheetCells.Key, out var key))
                    continue;

                var translations = new Dictionary<string, string>();
                translationRow.RemoveAt((int) TranslationsSheetCells.Key);
                
                for (var index = 0; index < translationRow.Count; index++)
                {
                    var translation = translationRow[index];

                    if (translation.ToString() == "" || !SpreadSheetUtils.IsRowHasCell(headers, index + 1, out var language ))
                        continue;

                    translations[language] = translation.ToString();
                }

                Dictionary[key] = translations;
            }

            if (!hotReload)
                return;
            
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(1);

            _timer = new Timer(_ =>
            {
                LoadValuesFromSpreadSheet(spreadSheet, hotReload: false);
            }, null, startTimeSpan, periodTimeSpan);
        }
    }
}