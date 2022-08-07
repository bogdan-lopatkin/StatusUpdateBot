using Newtonsoft.Json.Linq;

namespace StatusUpdateBot.DataSources.Json
{
    public interface IJson
    {
        JObject GetContents();

        void Save();
    }
}