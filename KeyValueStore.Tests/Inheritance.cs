using System;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class Inheritance
    {
        [TestMethod]
        public void Interfaces()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Product)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();

            IProduct product = new Product { Id = id, Text = "abc" };
            datacontext.Create(product);

            Product product2 = new Product { Id = id, Text = "def" };
            datacontext.Update(product2);

            var result = datacontext.Read<Product>(id);

            Assert.AreEqual("def", result.Text);
        }

        [TestMethod]
        public void ReadInterface()
        {
            //var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(ProductA), typeof(ProductB), typeof(Product)).Create();
            //datacontext.EnsureNewDatabase();

            //var p1 = Guid.NewGuid();
            //datacontext.Create(new ProductA {Id = p1, Text = "abc", ValueA = "va" });

            //IProduct product = datacontext.Read<IProduct>(p1);

            //Assert.IsNotNull(product);
        }

        [TestMethod]
        public void ChildrenAndInheritance()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order), typeof(ProductA), typeof(ProductB)).Create();
            datacontext.EnsureNewDatabase();

            var order = new Order() {Id = Guid.NewGuid()};
            datacontext.Create(order);

            var p1 = new ProductA {Id = Guid.NewGuid(), Text = "abc", ValueA = "va"};
            datacontext.Create(p1);
            var p2 = new ProductB {Id = Guid.NewGuid(), Text = "abc", ValueB = "vb"};
            datacontext.Create(p2);

            datacontext.AppendChild(order, p1);
            datacontext.AppendChild(order, p2);

            var children = datacontext.GetChildren<Order, IProduct>(order);
            Assert.AreEqual(2, children.Count);
            var children2 = datacontext.GetChildren<Order, Product>(order);
            Assert.AreEqual(2, children2.Count);
        }
    }
}
