﻿using System;
using PoorMan.KeyValueStore.Annotation;

namespace KeyValueStore.Tests.Dto
{
    public interface IProduct
    {
        string Text { get; set; }
    }

    public class Product : IProduct
    {
        public string Text { get; set; }
        [Id]
        public Guid Id { get; set; }
    }

    public class ProductA : Product
    {
        public string ValueA { get; set; }
    }

    public class ProductB : Product
    {
        public string ValueB { get; set; }
    }
}