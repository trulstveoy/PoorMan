using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using LoadTests.Dto;
using PoorMan.KeyValueStore;

namespace LoadTests
{
    class Program
    {
        private const string Connectionstring = "Data Source=localhost;Initial Catalog=PoorMan;Integrated Security=True;Pooling=False";

        static void Main()
        {
            Console.WriteLine("Startup");

            var configuration = new Configuration(Connectionstring).WithDocuments(typeof(Order), typeof(Product));

            configuration.Create().EnsureNewDatabase();

            var orders = GetOrders();

            var context = configuration.Create();
            Console.WriteLine("Write");
            Parallel.ForEach(orders, order =>
            {
                using(var transaction = new TransactionScope())
                { 
                    context.Insert(order);
                    for (int i = 0; i < 5; i++)
                    {
                        var product = new Product() {Id = Guid.NewGuid().ToString(), Text = GetText()};
                        context.Insert(product);
                        context.AppendChild(order, product);
                    }
                    transaction.Complete();
                }
                Console.Write(".");
            });

            Console.WriteLine("Read");
            Parallel.ForEach(orders.Select(x => x.Id), id =>
            {
                var order = context.Read<Order>(id);
                var product = order.Products.First();
                Console.Write(".");
            });

            Console.WriteLine("End");
            Console.ReadKey();
        }

        private static List<Order> GetOrders()
        {
            return Enumerable.Range(0, 100).Select(x => new Order() {Id = Guid.NewGuid(), Text = GetText()}).ToList();
        }

        private static string GetText()
        {
            return string.Join("", Enumerable.Range(0, 20).Select(x => Guid.NewGuid()));
        }
    }
}
