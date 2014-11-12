using System;
using PoorMan.KeyValueStore.Annotation;

namespace KeyValueStore.Tests.Dto
{
    public interface IProduct
    {
        string Text { get; set; }
    }

    public class Product : IProduct
    {
        [Id]
        public virtual Guid Id { get; set; }

        public virtual string Text { get; set; }
    }

    public class ProductA : Product
    {
        public virtual string ValueA { get; set; }
    }

    public class ProductB : Product
    {
        public virtual string ValueB { get; set; }
    }
}