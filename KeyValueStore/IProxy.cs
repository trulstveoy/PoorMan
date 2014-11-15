using System.Security.Cryptography.X509Certificates;

namespace PoorMan.KeyValueStore
{
    public interface IProxy
    {
        object GetInstance();
    }
}