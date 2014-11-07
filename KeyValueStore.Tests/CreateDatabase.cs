using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class CreateDatabase
    {
        [TestMethod]
        public void Go()
        {
            var context = new Configuration(Constants.Connectionstring).WithDocuments().Create();
            context.EnsureNewDatabase();
        }
    }
}