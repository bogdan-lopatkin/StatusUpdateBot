using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace StatusUpdateBot.Translators.External
{
    public class LibreTranslateTranslator : IExternalTranslator
    {
        public string Translate(string text, string to, string from = null)
        {
            HttpClient client = new HttpClient();

            var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    {"q", text},
                    {"target", to},
                    {"source", from ?? "auto"},
                    {"format", "text"},
                }
            );

            var response = client.PostAsync("https://libretranslate.de/translate", content).Result;

            return response.IsSuccessStatusCode 
                ? JsonConvert.DeserializeObject<TranslatorResponse>(response.Content.ReadAsStringAsync().Result)?.TranslatedText
                : text;
        }

        public string[] TranslateBatch(string[] textStrings, string to, string from = "auto")
        {
            string separator = $"{Environment.NewLine} - ";
            
            return Translate(String.Join(separator, textStrings), to, from).Split(separator);
        }
        
        private class TranslatorResponse
        {
            [JsonProperty("translatedText")]
            public string TranslatedText { get; set; }
        }
    }
}