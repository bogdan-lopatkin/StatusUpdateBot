using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace StatusUpdateBot.Translators.External
{
    public class MicrosoftTranslator : IExternalTranslator
    {
        private const string DefaultRegion = "GLOBAL";

        private readonly string _apiEndpoint;
        private readonly string _apiKey;
        private readonly string _region;
        private readonly HttpClient _client;

        public MicrosoftTranslator(string apiEndpoint, string apiKey, string region = DefaultRegion)
        {
            _apiEndpoint = apiEndpoint;
            _apiKey = apiKey;
            _region = region;
            
            _client = new HttpClient();
        }
        public string Translate(string text, string to, string from = null)
        {
            string queryParams = $"/translate?api-version=3.0&to={to}";

            if (from != null)
                queryParams += $"&from={from}";

            var requestBody = JsonConvert.SerializeObject(new object[] { new { Text = text } });
            
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(_apiEndpoint + queryParams);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
                
            var response = _client.SendAsync(request).Result;

            return response.IsSuccessStatusCode 
                ? JsonConvert.DeserializeObject<TranslatorResponse[]>(response.Content.ReadAsStringAsync().Result)?
                    .First().Translations.First().Text
                : text;
        }

        public string[] TranslateBatch(string[] textStrings, string to, string from = null)
        {
            string separator = $"{Environment.NewLine} - ";
            
            return Translate(String.Join(separator, textStrings), to, from).Split(separator);
        }

        private class DetectedLanguage
        {
            public string Language { get; set; }
            public double Score { get; set; }
        }

        private class Translation
        {
            public string Text { get; set; }
            public string To { get; set; }
        }

        private class TranslatorResponse
        {
            public DetectedLanguage DetectedLanguage { get; set; }
            public List<Translation> Translations { get; set; }
        }
    }
}