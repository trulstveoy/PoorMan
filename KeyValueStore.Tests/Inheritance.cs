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

            Product product2 = new Product { Text = "def" };
            datacontext.Update(id, product2);

            var result = datacontext.Read<Product>(id);

            Assert.AreEqual("def", result.Text);
        }

        [TestMethod]
        public void ReadInterface()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(ProductA)).Create();
            datacontext.EnsureNewDatabase();

            var p1 = Guid.NewGuid();
            datacontext.Create(new ProductA {Id = p1, Text = "abc", ValueA = "va" });

            IProduct product = datacontext.Read<Product>(p1);

            Assert.IsNotNull(product);
        }

        [TestMethod]
        public void ChildrenAndInheritance()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order), typeof(ProductA), typeof(ProductB)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            datacontext.Create(new Order() {Id = id});

            var p1 = Guid.NewGuid();
            datacontext.Create(new ProductA {Id = p1, Text = "abc", ValueA = "va" });
            var p2 = Guid.NewGuid();
            datacontext.Create(new ProductB {Id = p2, Text = "abc", ValueB = "vb" });

            datacontext.AppendChild<Order, ProductA>(id, p1);
            datacontext.AppendChild<Order, ProductB>(id, p2);

            var children = datacontext.GetChildren<IProduct>(id);
            Assert.AreEqual(2, children.Count);
            var children2 = datacontext.GetChildren<Product>(id);
            Assert.AreEqual(2, children2.Count);
        }
    }
}
