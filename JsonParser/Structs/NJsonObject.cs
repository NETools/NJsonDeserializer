using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser.Structs
{
    public enum NJsonObjectType
    {
        Number,
        String,
        Class,
        List
    }

    internal class NJsonObject
    {
        private Dictionary<string, NJsonObject> _classes = new Dictionary<string, NJsonObject>();
        private List<NJsonObject> _objects = new List<NJsonObject>();
        public string Value { get; set; }
        public NJsonObjectType ObjectType { get; set; }

        public NJsonObject this[string name] => _classes[name];
        public NJsonObject this[int index] => _objects[index];

        internal void AddClass(string name, NJsonObject value)
        {
            _classes[name] = value;
        }

        internal void AddObject(NJsonObject value)
        {
            _objects.Add(value);
        }
        
    }
}
