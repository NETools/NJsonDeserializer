using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonParser.Structs;
using JsonParser.Tokenizer;

namespace JsonParser.Core
{
    public class NJson
    {
        private RuleMatching _rules = new RuleMatching();

        public RuleMatching Rules => _rules;

        public NJson()
        {
            _rules.AddProcessor(ProcessVariableNotation,
                (t) => t.Namespace == "System");
            _rules.AddProcessor(ProcessClassNotation,
                (t) => t.Namespace != "System");
            _rules.AddProcessor(ProcessTuple,
                (t) => t.Namespace == "System",
                (t) => t.Name.Contains(typeof(Tuple).Name));
            _rules.AddProcessor(ProcessList,
                (t) => t.Namespace == "System.Collections.Generic",
                (t) => t.Name.Contains("List"));
            _rules.AddProcessor(ProcessDictionary,
                (t) => t.Namespace == "System.Collections.Generic",
                (t) => t.Name.Contains("Dictionary"));
            _rules.AddProcessor(ProcessObject,
                (t) => t.Namespace == "System",
                (t) => t == typeof(object));

            _rules.AddMapper(
                (name) => $"m_{name}",
                (t) => t.Namespace == "System",
                (t) => t.Name.Contains(typeof(Tuple).Name));
        }

        public string SerializeInstance(object instance)
        {
            return JsonSerializer.Serialize(instance);
        }

        public void DeserializeIntoInstance(string json, object instance, Func<Type, NJsonInstanciatorResult> classInstanciator = null)
        {
            if(classInstanciator == null)
            {
                classInstanciator = (type) => new NJsonInstanciatorResult() { Code = NJsonInstanciatorResultCode.Error };
            }

            var instanceType = instance.GetType();
            var mapper = _rules.MatchMapper(instanceType);
            var properties = instanceType.GetProperties().ToList();
            var tokenStream = JsonTokenizer.NormalizeTokenStream(JsonTokenizer.TokenizeJson(json)).GetEnumerator();
            tokenStream.MoveNext(); // {

            while (tokenStream.MoveNext())
            {
                var currentToken = tokenStream.Current;
                if (currentToken.TokenType == TokenType.QuoteSign) // We expect the property name after this
                {
                    tokenStream.MoveNext();
                    var propertyName = tokenStream.Current.Value;
                    var propertyInfo = properties.Find(p => p.Name.Equals(propertyName));

                    tokenStream.MoveNext(); // name
                    tokenStream.MoveNext(); // "
                    if (tokenStream.Current.TokenType != TokenType.Colon)
                        break;

                    var processor = _rules.MatchProcessor(propertyInfo.PropertyType);
                    var parsedValue = processor.Invoke(propertyInfo.PropertyType, tokenStream, classInstanciator);

                    if (propertyInfo.SetMethod != null)
                    {
                        propertyInfo.SetValue(instance, parsedValue);
                    }
                    else
                    {
                        var mappedName = mapper.Invoke(propertyName);
                        var fieldInfo = instanceType.GetRuntimeFields().ToList().Find(p => p.Name.Equals(mappedName));
                        if (fieldInfo != null)
                            fieldInfo.SetValue(instance, parsedValue);
                    }
                }
            }
        }

        private object ProcessObject(Type currentType, IEnumerator<JsonToken> tokenStream, Func<Type, NJsonInstanciatorResult> classInstanciator)
        {
            var jsonObject = new NJsonObject();
            tokenStream.MoveNext();
            switch (tokenStream.Current.TokenType)
            {
                case TokenType.String:
                    jsonObject.Value = tokenStream.Current.Value;
                    jsonObject.ObjectType = NJsonObjectType.Number;
                    break;
                case TokenType.QuoteSign:
                    tokenStream.MoveNext();
                    jsonObject.Value = tokenStream.Current.Value;
                    tokenStream.MoveNext();
                    jsonObject.ObjectType = NJsonObjectType.String;
                    break;
                case TokenType.BracketOpen:
                    var subJson = ReadBetweenBrackets(tokenStream);
                    var subTokenStream = JsonTokenizer.NormalizeTokenStream(JsonTokenizer.TokenizeJson(subJson)).GetEnumerator();
                    subTokenStream.MoveNext();
                    while (subTokenStream.MoveNext())
                    {
                        if (subTokenStream.Current.TokenType == TokenType.Comma)
                            subTokenStream.MoveNext();
                        if (subTokenStream.Current.TokenType != TokenType.QuoteSign)
                            break;
                        subTokenStream.MoveNext();
                        string variableName = subTokenStream.Current.Value;
                        subTokenStream.MoveNext();
                        subTokenStream.MoveNext();

                        jsonObject.AddClass(variableName, (NJsonObject) ProcessObject(null, subTokenStream, classInstanciator));
                    }
                    jsonObject.ObjectType = NJsonObjectType.Class;
                    break;
                case TokenType.BraceOpen:
                    jsonObject.AddObject((NJsonObject)ProcessObject(null, tokenStream, classInstanciator));

                    while (tokenStream.MoveNext() && tokenStream.Current.TokenType != TokenType.BraceClose)
                    {
                        jsonObject.AddObject((NJsonObject)ProcessObject(null, tokenStream, classInstanciator));
                    }

                    jsonObject.ObjectType = NJsonObjectType.List;
                    break;
            }

            return jsonObject;
        }

        private object ProcessDictionary(Type currentType, IEnumerator<JsonToken> tokenStream, Func<Type, NJsonInstanciatorResult> classInstaciator)
        {
            var keyType = currentType.GenericTypeArguments[0];
            var valueType = currentType.GenericTypeArguments[1];

            var keyProcessor = _rules.MatchProcessor(keyType);
            var valueProcessor = _rules.MatchProcessor(valueType);

            var dictionaryType = typeof(Dictionary<,>);
            var genericDictionaryType = dictionaryType.MakeGenericType(keyType, valueType);
            var dictionary = (IDictionary)Activator.CreateInstance(genericDictionaryType);

            while (tokenStream.MoveNext() && tokenStream.Current.TokenType != TokenType.BracketClose)
            {
                var parsedKey = keyProcessor.Invoke(keyType, tokenStream, classInstaciator);
                tokenStream.MoveNext();
                if (tokenStream.Current.TokenType != TokenType.Colon)
                    break;

                var parsedValue = valueProcessor.Invoke(valueType, tokenStream, classInstaciator);

                dictionary.Add(parsedKey, parsedValue);
            }


            return dictionary;
        }

        private object ProcessList(Type currentType, IEnumerator<JsonToken> tokenStream, Func<Type, NJsonInstanciatorResult> classInstaciator)
        {
            var listGenericType = currentType.GenericTypeArguments[0];
            var processor = _rules.MatchProcessor(listGenericType);

            var listType = typeof(List<>);
            var genericTypeList = listType.MakeGenericType(listGenericType);
            var list = (IList)Activator.CreateInstance(genericTypeList);

            while (tokenStream.MoveNext())
            {
                var currentToken = tokenStream.Current;
                if (currentToken.TokenType == TokenType.BraceClose)
                    break;
                var parsedValue = processor.Invoke(listGenericType, tokenStream, classInstaciator);
                list.Add(parsedValue);
            }

            return list;
        }

        private object ProcessTuple(Type currentType, IEnumerator<JsonToken> tokenStream, Func<Type, NJsonInstanciatorResult> classInstaciator)
        {
            return ProcessClassNotation(currentType, tokenStream, (type) =>
            {
                NJsonInstanciatorResult rslt = new NJsonInstanciatorResult();
                if (type.Name.Contains(typeof(Tuple).Name))
                {
                    object[] ctorParams = new object[type.GenericTypeArguments.Length];
                    var instance = Activator.CreateInstance(type, ctorParams);

                    rslt.Code = NJsonInstanciatorResultCode.Success;
                    rslt.Value = instance;
                    return rslt;
                }
                return classInstaciator(type);
            });
        }

        private object ProcessVariableNotation(Type currentType, IEnumerator<JsonToken> tokenStream, Func<Type, NJsonInstanciatorResult> classInstaciator)
        {
            tokenStream.MoveNext();
            bool isParsingString = false;
            if (tokenStream.Current.TokenType == TokenType.QuoteSign)
            {
                isParsingString = true;
                tokenStream.MoveNext();
            }

            var value = ParseType(currentType, tokenStream.Current.Value);
            if (isParsingString)
                tokenStream.MoveNext();

            return value;
        }

        private object ProcessClassNotation(Type currentType, IEnumerator<JsonToken> tokenStream, Func<Type, NJsonInstanciatorResult> classInstaciator)
        {
            tokenStream.MoveNext();

            if (tokenStream.Current.TokenType != TokenType.BracketOpen) // we expect a bracket open due to non system type thus custom class
                return null;

            var subJson = ReadBetweenBrackets(tokenStream);

            var instanciationResult = classInstaciator(currentType);
            if (instanciationResult.Code == NJsonInstanciatorResultCode.Failed)
            {
                instanciationResult.Value = Activator.CreateInstance(currentType);
            }

            DeserializeIntoInstance(subJson, instanciationResult.Value, classInstaciator);

            return instanciationResult.Value;
        }

        /// <summary>
        /// Tokenstream must start at open bracket ({)
        /// </summary>
        /// <param name="tokenStream"></param>
        /// <returns></returns>
        private string ReadBetweenBrackets(IEnumerator<JsonToken> tokenStream)
        {
            int brackets = 1;
            StringBuilder subJsonBuilder = new StringBuilder();
            subJsonBuilder.Append(tokenStream.Current.Value);
            while (tokenStream.MoveNext())
            {
                var current = tokenStream.Current;
                if (current.TokenType == TokenType.BracketOpen)
                    brackets++;
                else if (current.TokenType == TokenType.BracketClose)
                    brackets--;

                subJsonBuilder.Append(current.Value);

                if (brackets == 0)
                    break;
            }

            return subJsonBuilder.ToString();
        }
        public object ParseType(Type propertyType, string value)
        {
            switch (propertyType)
            {
                case Type prop when prop == typeof(int):
                    return int.Parse(value);
                case Type prop when prop == typeof(double):
                    return double.Parse(value.Replace(".", ","));
                case Type prop when prop == typeof(string):
                    return value;
                case Type prop when prop == typeof(long):
                    return long.Parse(value);
                case Type prop when prop == typeof(byte):
                    return byte.Parse(value);
                case Type prop when prop == typeof(sbyte):
                    return sbyte.Parse(value);
                case Type prop when prop == typeof(short):
                    return short.Parse(value);
                case Type prop when prop == typeof(ushort):
                    return ushort.Parse(value);
                case Type prop when prop == typeof(uint):
                    return uint.Parse(value);
                case Type prop when prop == typeof(ulong):
                    return ulong.Parse(value);
                case Type prop when prop == typeof(float):
                    return float.Parse(value);
                case Type prop when prop == typeof(decimal):
                    return decimal.Parse(value);
                case Type prop when prop == typeof(char):
                    return char.Parse(value);
                case Type prop when prop == typeof(decimal):
                    return decimal.Parse(value);
                case Type prop when prop == typeof(bool):
                    return bool.Parse(value);
                case Type prop when prop == typeof(DateTime):
                    return DateTime.Parse(value);

            }

            return null;
        }

    }
}
