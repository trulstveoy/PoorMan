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
            context.Insert(order);

            for (int i = 0; i < 10; i++)
            {
                var product = new Product {Id = Guid.NewGuid(), Text = "child"};
                context.Insert(product);
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
            context.Insert(order);

            var products = Enumerable.Range(0, 10).Select(x => new Product() {Id = Guid.NewGuid(), Text = "child" }).ToList();
            foreach (var product in products)
            {
                context.Insert(product);
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

        [TestMethod]
        public void AppendRemoveWithProxies()
        {
            var context = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Product), typeof(Order)).Create();
            context.EnsureNewDatabase();

            var orderId = Guid.NewGuid();  
            var productId = Guid.NewGuid();
            context.Insert(new Order { Id = orderId, Text = "parent" });
            context.Insert(new Product() {Id = productId, Text = "child"});

            var order = context.Read<Order>(orderId);
            var product = context.Read<Product>(productId);

            context.AppendChild(order, product);
            Assert.AreEqual(1, context.GetChildren<Order, Product>(order).Count);

            context.RemoveChild(order, product);
            Assert.AreEqual(0, context.GetChildren<Order, Product>(order).Count);
        }
    }
}