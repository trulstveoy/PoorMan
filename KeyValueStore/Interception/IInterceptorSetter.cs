using System.Security.Cryptography.X509Certificates;

namespace PoorMan.KeyValueStore.Interception
{
    public interface IInterceptorSetter
    {
        void SetInterceptor(ICallInterceptor interceptor);
    }
}