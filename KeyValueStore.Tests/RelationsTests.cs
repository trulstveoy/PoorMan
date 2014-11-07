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
        [TestMethod]
        public void CreateChild()
        {
            var context = new Configuration(Constants.Connectionstring)
                .WithDocuments(typeof(Order), typeof(Product)).Create();

            context.EnsureNewDatabase();

            var order = new Order { Id = new Guid(), Text = "parent" };
            context.Create(order);

            for (int i = 0; i < 10; i++)
            {
                var product = new Product {Id = Guid.NewGuid(), Text = "child"};
                context.Create(product);
                context.AppendChild(order, product);
            }


            var children = context.GetChildren<Order, Product>(order);
            Assert.AreEqual(10, children.Count);
            Assert.AreEqual("child", children.First().Text);
        }

        [TestMethod]
        public void RemoveChild()
        {
            var context = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Product), typeof(Order)).Create();

            context.EnsureNewDatabase();

            var order = new Order {Id = Guid.NewGuid(), Text = "parent"};
            context.Create(order);

            var products = Enumerable.Range(0, 10).Select(x => new Product() {Id = Guid.NewGuid(), Text = "child" }).ToList();
            foreach (var product in products)
            {
                context.Create(product);
                context.AppendChild(order, product);
            }

            foreach (var product in products.Take(5))
            {
                context.RemoveChild<Order, Product>(order, product);
            }

            var children = context.GetChildren<Order, Product>(order);
            Assert.AreEqual(5, children.Count);
            Assert.AreEqual("child", children.First().Text);
        }
    }
}