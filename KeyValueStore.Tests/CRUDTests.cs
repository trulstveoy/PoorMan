using System;
using System.Linq;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class CRUDTests
    {
        [TestMethod]
        public void CreateRead()
        {
            var datacontext = new Configuration(Constants.Connectionstring).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var order = new Order {Text = "Abc"};

            datacontext.Create(id, order);

            var order2 = datacontext.Read<Order>(id);

            Assert.AreEqual(order.Text, order2.Text);
        }

        [TestMethod]
        public void ReadNull()
        {
            var datacontext = new Configuration(Constants.Connectionstring).Create();
            datacontext.EnsureNewDatabase();
            
            var order = datacontext.Read<Order>(Guid.NewGuid());

            Assert.IsNull(order);
        }

        [TestMethod]
        public void Update()
        {
            var datacontext = new Configuration(Constants.Connectionstring).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            
            datacontext.Create(id, new Order { Text = "Abc" });
            datacontext.Update(id, new Order() {Text = "Def"});

            var result = datacontext.Read<Order>(id);
            Assert.AreEqual("Def", result.Text);
        }

        [TestMethod]
        public void ReadAll()
        {
            var datacontext = new Configuration(Constants.Connectionstring).Create();
            datacontext.EnsureNewDatabase();

            for (int i = 0; i < 2000; i++)
            {
                datacontext.Create(Guid.NewGuid(), new Product {Text = "abc"});
            }

            var products = datacontext.ReadAll<Product>();
            Assert.IsTrue(products.All(x => new[] {"abc", "def", "ghi"}.Contains(x.Text)));
        }

        [TestMethod]
        public void Delete()
        {
            var datacontext = new Configuration(Constants.Connectionstring).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            datacontext.Create(id, new Product{ Text = "abc"});

            datacontext.Delete<Product>(id);

            Assert.IsNull(datacontext.Read<Product>(id));
        }
    }
}
