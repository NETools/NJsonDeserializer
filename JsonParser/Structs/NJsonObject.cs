using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser.Structs
{
    internal class NJsonObject
    {
        private Dictionary<string, NJsonObject> _classes = new Dictionary<string, NJsonObject>();
        public string Value { get; set; }

        public NJsonObject this[string name]=> _classes[name];
        internal void AddClass(string name, NJsonObject value)
        {
            _classes[name] = value;
        }
        
    }
}
