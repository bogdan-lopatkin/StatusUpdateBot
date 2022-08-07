using System.IO;
using Newtonsoft.Json.Linq;

namespace StatusUpdateBot.DataSources.Json
{
    public class Json: IJson
    {
        private readonly string _filePath;
        private readonly JObject _contents;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public Json(string filePath = @"statuses.json")
        {
            _filePath = filePath;
            
            if (!File.Exists(_filePath))
                File.Create(_filePath).Dispose();

            var fileContents = File.ReadAllText(filePath);

            _contents = fileContents.Length > 0 ? JObject.Parse(File.ReadAllText(filePath)) : new JObject();
        }

        public JObject GetContents()
        {
            return _contents;
        }

        public void Save()
        {
            File.WriteAllText(_filePath,_contents.ToString());
        }
    }
}