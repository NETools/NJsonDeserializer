using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser.Tokenizer
{
    public enum TokenType
    {
        BracketOpen,
        BracketClose,
        Comma,
        QuoteSign,
        Colon,
        String,
        BraceOpen,
        BraceClose
    };

    public class JsonToken
    {
        public TokenType TokenType;
        public string Value;

        public override string ToString()
        {
            return $"[{TokenType}] {Value}";
        }
    }
}
