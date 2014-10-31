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
            var datacontext = new DataContext(Constants.Connectionstring);
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();

            IProduct product = new Product { Text = "abc" };
            datacontext.Create(id, product);

            Product product2 = new Product { Text = "def" };
            datacontext.Update(id, product2);

            var result = datacontext.Read<Product>(id);

            Assert.AreEqual("def", result.Text);
        }

        [TestMethod]
        public void ChildrenAndInheritance()
        {
            //var datacontext = new DataContext(Constants.Connectionstring);
            //datacontext.EnsureNewDatabase();

            //var id = Guid.NewGuid();
            //datacontext.Create(id, new Order());

            //var p1 = Guid.NewGuid();
            //datacontext.Create(p1, new ProductA {Text = "abc", ValueA = "va"});
            //var p2 = Guid.NewGuid();
            //datacontext.Create(p2, new ProductB {Text = "abc", ValueB = "vb"});

            //datacontext.AppendChild<Order, ProductA>(id, p1);
            //datacontext.AppendChild<Order, ProductA>(id, p2);

            //var children = datacontext.GetChildren<IProduct>(id);
        }
    }
}
