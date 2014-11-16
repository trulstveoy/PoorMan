using System;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests.Dto
{
    public class OrderLine
    {
        [Id]
        public virtual Guid Id { get; set; }

        public virtual Guid OrderId { get; set; }

        [Parent]
        public virtual Order GetOrder(object id)
        {
            return null;
        }
    }
}