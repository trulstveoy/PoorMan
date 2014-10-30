using System;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class CRUDTests
    {
        private const string Connectionstring = "Data Source=localhost;Initial Catalog=PoorMan;Integrated Security=True;Pooling=False";

        [TestMethod]
        public void CreateRead()
        {
            var datacontext = new DataContext(Connectionstring);
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
            var datacontext = new DataContext(Connectionstring);
            datacontext.EnsureNewDatabase();
            
            var order = datacontext.Read<Order>(Guid.NewGuid());

            Assert.IsNull(order);
        }

        [TestMethod]
        public void Update()
        {
            var datacontext = new DataContext(Connectionstring);
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            
            datacontext.Create(id, new Order { Text = "Abc" });
            datacontext.Update(id, new Order() {Text = "Def"});

            var result = datacontext.Read<Order>(id);
            Assert.AreEqual("Def", result.Text);
        }

        [TestMethod]
        public void Interfaces()
        {
            var datacontext = new DataContext(Connectionstring);
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();

            IProduct product = new Product {Text = "abc"};
            datacontext.Create(id, product);

            Product product2 = new Product { Text = "def" };
            datacontext.Update(id, product2);

            var result = datacontext.Read<Product>(id);

            Assert.AreEqual("def", result.Text);
        }
    }
}
