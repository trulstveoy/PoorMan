using System;
using System.Linq;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class RelationsTests
    {
        private const string Connectionstring = "Data Source=localhost;Initial Catalog=PoorMan;Integrated Security=True;Pooling=False";

        [TestMethod]
        public void CreateChild()
        {
            var context = new DataContext(Connectionstring);

            context.EnsureNewDatabase();

            var orderId = Guid.NewGuid();
            context.Create(orderId, new Order {Text = "parent"});

            for (int i = 0; i < 10; i++)
            {
                var productId = Guid.NewGuid();
                context.Create(productId, new Product() { Text = "child" });
                context.AppendChild(orderId, productId);
            }


            var children = context.GetChildren<Product>(orderId);
            Assert.AreEqual(10, children.Count);
            Assert.AreEqual("child", children.First().Text);
        }
    }
}