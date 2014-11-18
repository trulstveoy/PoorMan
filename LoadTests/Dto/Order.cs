using System;
using System.Collections.Generic;
using PoorMan.KeyValueStore;

namespace LoadTests.Dto
{
    public class Order
    {
        [Id]
        public virtual Guid Id { get; set; }
        public virtual string Text { get; set; }

        [Child]
        public virtual List<Product> Products { get; set; }
    }
}