using System;
using System.Collections.Generic;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class LazyLoad
    {
        [TestMethod]
        public void LazyLoadChildren()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order), typeof(Product)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var order = new Order() {Id = id, Text = "Abc"};
            var p1 = new Product() { Id = Guid.NewGuid(), Text = "P1" };
            var p2 = new Product() { Id = Guid.NewGuid(), Text = "P2" };

            datacontext.Create(order);
            datacontext.Create(p1);
            datacontext.Create(p2);
            datacontext.AppendChild(order, p1);
            datacontext.AppendChild(order, p2);

            var result = datacontext.Read<Order>(id);
            List<Product> products = result.Products;
            Assert.AreEqual(2, products.Count);
        }

        [TestMethod]
        public void LazyLoadParent()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order), typeof(OrderLine)).Create();
            datacontext.EnsureNewDatabase();
            
            var order = new Order() { Id = Guid.NewGuid(), Text = "Abc" };
            var line = new OrderLine() { Id = Guid.NewGuid(), OrderId = order.Id };
            
            datacontext.Create(order);
            datacontext.Create(line);
            datacontext.AppendChild(order, line);

            var result = datacontext.Read<OrderLine>(line.Id);
            var resultOrder = result.GetOrder(result.OrderId);
            Assert.IsNotNull(resultOrder);
            Assert.AreEqual("Abc", resultOrder.Text);
            Assert.AreEqual(1, resultOrder.OrderLines.Count);
        }
    }
}