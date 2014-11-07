using System;
using System.Collections.Generic;
using PoorMan.KeyValueStore.Annotation;
using PoorMan.KeyValueStore.Interception;

namespace KeyValueStore.Tests.Dto
{
    public class Order
    {
        public string Text { get; set; }

        [Child]
        public virtual List<Product> Products { get; set; }

        [Id]
        public Guid Id { get; set; }
    }
}