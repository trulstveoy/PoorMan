using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class CreateDatabase
    {
        private const string Connectionstring = "Data Source=localhost;Initial Catalog=PoorMan;Integrated Security=True;Pooling=False";

        [TestMethod]
        public void Go()
        {
            var context = new DataContext(Connectionstring);
            context.EnsureNewDatabase();
        }
    }
}