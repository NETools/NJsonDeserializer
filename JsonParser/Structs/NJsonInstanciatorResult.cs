using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser.Structs
{
    public enum NJsonInstanciatorResultCode
    {
        Success,
        Error,
        Failed
    }

    public struct NJsonInstanciatorResult
    {
        public NJsonInstanciatorResultCode Code;
        public object Value;
    }
}
