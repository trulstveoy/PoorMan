using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeyValueStore.Tests
{
    public class MyInterceptor : IInterceptor
    {
        private readonly object _instance;

        public MyInterceptor(object instance)
        {
            _instance = instance;
        }
        
        public void Intercept(IInvocation invocation)
        {
            invocation.ReturnValue = invocation.Method.Invoke(_instance, invocation.Arguments);
        }
    }

    [TestClass]
    public class Proxy
    {
        [TestMethod]
        public void Foo()
        {
            var proxyGenerator = new ProxyGenerator();

            var order = new Order {Id = Guid.NewGuid(), Text="Abc"};
            
            var proxy = (Order)proxyGenerator.CreateClassProxy(typeof (Order), new MyInterceptor(order));

            Assert.AreEqual(order.Id, proxy.Id);
        }
    }
}
