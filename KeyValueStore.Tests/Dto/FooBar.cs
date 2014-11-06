using System.Xml.Serialization;

namespace KeyValueStore.Tests.Dto
{
    public class Foo
    {
        public int Num { get; set; }
        public virtual Bar Bar { get; set; }
    }
    
    public class Foo2 : Foo
    {
        public string Text { get; set; }
    }

    public class Bar
    {
        public string Text { get; set; }    
    }
}