namespace KeyValueStore.Tests.Dto
{
    public interface IProduct
    {
        string Text { get; set; }
    }

    public class Product : IProduct
    {
        public string Text { get; set; } 
    }
}