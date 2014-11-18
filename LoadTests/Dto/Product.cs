using PoorMan.KeyValueStore;

namespace LoadTests.Dto
{
    public class Product
    {
        [Id]
        public virtual string Id { get; set; }
        public virtual string Text { get; set; } 
    }
}