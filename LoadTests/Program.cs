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

            var configuration = new Configuration(Connectionstring);

            configuration.Create().EnsureNewDatabase();

            var orders = GetOrders();

            Parallel.ForEach(orders, order =>
            {
                using(var transaction = new TransactionScope())
                { 
                    var context =  configuration.Create();
                    context.Insert(order);
                    transaction.Complete();
                }
                Console.Write(".");
            });

            Console.WriteLine("End");
            Console.ReadKey();
        }

        private static List<Order> GetOrders()
        {
            return Enumerable.Range(0, 10000).Select(x => new Order() {Id = Guid.NewGuid(), Text = GetText()}).ToList();
        }

        private static string GetText()
        {
            return string.Join("", Enumerable.Range(0, 1000).Select(x => Guid.NewGuid()));
        }
    }
}
