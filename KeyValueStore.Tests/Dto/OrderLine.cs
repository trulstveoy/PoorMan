using System;
using PoorMan.KeyValueStore.Annotation;

namespace KeyValueStore.Tests.Dto
{
    public class OrderLine
    {
        [Id]
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        [ParentId("OrderId")]
        public virtual Order Order { get; set; }
    }
}