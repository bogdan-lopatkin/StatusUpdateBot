using System.Collections.Generic;
using System.Linq;

namespace StatusUpdateBot.Utils
{
    public class StringFormatter
    {
        private readonly string _string;
        private readonly Dictionary<string, object> _parameters;

        public StringFormatter(string str){
            _string = str;
            _parameters = new Dictionary<string, object>();
        }

        public StringFormatter Add(string key, object val){
            _parameters.Add(key, val);

            return this;
        }

        public override string ToString()
        {
            return _parameters.Aggregate(
                _string,
                (current, parameter)
                    => current.Replace(parameter.Key, parameter.Value.ToString()));
        }
    }
}