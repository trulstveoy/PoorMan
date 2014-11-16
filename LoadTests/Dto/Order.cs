using System;
using PoorMan.KeyValueStore;

namespace LoadTests.Dto
{
    public class Order
    {
        public string Text { get; set; }
        [Id]
        public Guid Id { get; set; }
    }
}