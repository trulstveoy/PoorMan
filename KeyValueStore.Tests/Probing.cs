using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;
using PoorMan.KeyValueStore.Annotation;

namespace KeyValueStore.Tests
{
    public class Foo
    {
        [Id]
        public string Key { get; set; }
        public string Text { get; set; }
    }

    public interface IFoo
    {
        string Key { get; set; }
        string Text { get; set; }
    }

    public class FooImpl : IFoo
    {
        [Id]
        public string Key { get; set; }
        public string Text { get; set; }
    }

    [TestClass]
    public class Probing
    {
        [TestMethod]
        public void SimpleClass()
        {
            new Configuration(Constants.Connectionstring).WithDocuments(typeof(Foo)).Create();
        }

        [TestMethod]
        public void InterfaceImplementation()
        {
            new Configuration(Constants.Connectionstring).WithDocuments(typeof(FooImpl)).Create();
        }
    }
}
