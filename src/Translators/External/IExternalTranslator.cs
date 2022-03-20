namespace StatusUpdateBot.Translators.External
{
    public interface IExternalTranslator
    {
        public string Translate(string text, string to, string from = null);
        
        public string[] TranslateBatch(string[] textStrings, string to, string from = null);
    }
}