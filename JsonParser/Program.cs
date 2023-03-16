using JsonParser.Core;
using JsonParser.Structs;
using System.Reflection;

Console.WriteLine("BismIllah");

NJson nJson = new NJson();

A a = new A();
a.Data = "rofl";
a.C = new C() { AA = 1.91 };
a.C.Tuple = new Tuple<string, string>("", "");
a.Tuple = new Tuple<string, C>("aklerg", new C() { AA = 1919191, Tuple = new Tuple<string, string>("rofl", "lol") });
a.Strings.Add("1");
a.Strings.Add("2");

a.Cs.Add(new C() { AA = 2, Tuple = new Tuple<string, string>("a", "b") });
a.Cs.Add(new C() { AA = 1, Tuple = new Tuple<string, string>("a", "ba") });

a.Test.Add(new List<string>(new string[] { "a, b, c, d, e, f" }));

a.Mappings.Add("a", new C() { AA = 5 });
a.Mappings.Add("b", new C() { AA = 6 });

var json = nJson.SerializeInstance(a);

A deserializedA = new A();
nJson.DeserializeIntoInstance(json, deserializedA, (type) =>
{
    NJsonInstanciatorResult rslt = new();
    rslt.Code = NJsonInstanciatorResultCode.Failed;

    return rslt;
});

Console.WriteLine();


class C
{
    public double AA { get; set; }
    public Tuple<string, string> Tuple { get; set; }
}

class B 
{
    public string Data { get; set; }
    public C C { get; set; }
}

class A
{

    public string Data { get; set; }
    public C C { get; set; }
    public Tuple<string, C> Tuple { get; set; }
    public List<string> Strings { get; set; } = new List<string>();
    public Dictionary<string, C> Mappings { get; set; } = new();
    public List<C> Cs { get; set; } = new List<C>();

    public List<List<string>> Test { get; set; } = new List<List<string>>();
   
}