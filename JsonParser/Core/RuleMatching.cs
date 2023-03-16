using JsonParser.Structs;
using JsonParser.Tokenizer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser.Core
{
    public class RuleMatching
    {
        private List<(List<Predicate<Type>> Rules, Func<Type, IEnumerator<JsonToken>, Func<Type, NJsonInstanciatorResult>, object> Processor)> _processors = new();
        private List<(List<Predicate<Type>> Rules, Func<string, string> Mapper)> _mappers = new();

        public void AddProcessor(Func<Type, IEnumerator<JsonToken>, Func<Type, NJsonInstanciatorResult>, object> processor, params Predicate<Type>[] rules)
        {
            _processors.Add((new List<Predicate<Type>>(rules), processor));
        }

        public void AddMapper(Func<string, string> mapper, params Predicate<Type>[] rules)
        {
            _mappers.Add((new List<Predicate<Type>>(rules), mapper));
        }

        public Func<string, string> MatchMapper(Type type, bool or = false)
        {
            return Match<Func<string, string>>(_mappers, type, or);
        }

        public Func<Type, IEnumerator<JsonToken>, Func<Type, NJsonInstanciatorResult>, object> MatchProcessor(Type type, bool or = false)
        {
            return Match<Func<Type, IEnumerator<JsonToken>, Func<Type, NJsonInstanciatorResult>, object>>(_processors, type, or);
        }

        private ReturnType Match<ReturnType>(IList list, Type type, bool or = false)
        {
            dynamic winningTuple = list[0];
            var maxMatches = 0;

            foreach (dynamic defs in list)
            {
                int matches = 0;
                bool result = !or;
                foreach (var rule in defs.Item1)
                {
                    if (or)
                        result |= rule.Invoke(type);
                    else result &= rule.Invoke(type);

                    if (result)
                        matches++;
                }

                if (result && matches > maxMatches)
                {
                    maxMatches = matches;
                    winningTuple = defs;
                }

            }
            return winningTuple.Item2;
        }

    }
}