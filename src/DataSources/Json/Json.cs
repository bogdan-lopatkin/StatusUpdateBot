using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spire.Xls;
using StatusUpdateBot.SpreadSheets;
using JsonSerializer = System.Text.Json.JsonSerializer;

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