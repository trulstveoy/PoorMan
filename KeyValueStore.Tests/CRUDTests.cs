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
            var datacontext = new Configuration(Constants.Connectionstring)
                .WithDocuments(typeof(Order)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var order = new Order {Id = id, Text = "Abc"};

            datacontext.Insert(order);
            var order2 = datacontext.Read<Order>(id);

            Assert.AreEqual(order.Text, order2.Text);
        }

        [TestMethod]
        public void WriteReadWriteRead()
        {
            var datacontext = new Configuration(Constants.Connectionstring)
                .WithDocuments(typeof(Order)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var order = new Order { Id = id, Text = "Abc" };
            datacontext.Insert(order);

            var order2 = datacontext.Read<Order>(id);
            Assert.AreEqual(order.Text, order2.Text);

            order2.Text = "Def";
            datacontext.Update(order2);

            var order3 = datacontext.Read<Order>(id);
            
            Assert.AreEqual(order2.Text, order3.Text);
        }

        [TestMethod]
        public void ReadNull()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order)).Create();
            datacontext.EnsureNewDatabase();
            
            var order = datacontext.Read<Order>(Guid.NewGuid());

            Assert.IsNull(order);
        }

        [TestMethod]
        public void Update()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            
            datacontext.Insert(new Order {Id = id, Text = "Abc" });
            datacontext.Update(new Order() {Id= id, Text = "Def"});

            var result = datacontext.Read<Order>(id);
            Assert.AreEqual("Def", result.Text);
        }

        [TestMethod]
        public void ReadAll()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Product)).Create();
            datacontext.EnsureNewDatabase();

            for (int i = 0; i < 10; i++)
            {
                datacontext.Insert(new Product {Id = Guid.NewGuid(), Text = "abc"});
            }

            var products = datacontext.ReadAll<Product>();
            Assert.IsTrue(products.All(x => new[] {"abc", "def", "ghi"}.Contains(x.Text)));
        }

        [TestMethod]
        public void Delete()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Product)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            datacontext.Insert(new Product{Id = id, Text = "abc"});

            datacontext.Delete<Product>(id);

            Assert.IsNull(datacontext.Read<Product>(id));
        }

        [TestMethod]
        public void Upsert()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Product)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            datacontext.Upsert(new Product{ Id = id, Text="abc"});

            Assert.AreEqual("abc", datacontext.Read<Product>(id).Text);

            datacontext.Upsert(new Product { Id = id, Text = "cba" });

            Assert.AreEqual("cba", datacontext.Read<Product>(id).Text);
        }
    }
}
