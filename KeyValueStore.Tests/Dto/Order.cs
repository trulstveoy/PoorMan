using System;
using System.Collections.Generic;
using PoorMan.KeyValueStore.Annotation;

namespace KeyValueStore.Tests.Dto
{
    public class Order
    {
        public virtual string Text { get; set; }

        [Child]
        public virtual List<Product> Products { get; set; }

        [Child]
        public virtual List<OrderLine> OrderLines { get; set; }

        [Id]
        public virtual Guid Id { get; set; }
    }
}