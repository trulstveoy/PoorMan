using System;
using System.Reflection;

namespace PoorMan.KeyValueStore.Interception
{
    public interface ICallInterceptor
    {
        object Invoke(object proxy, MethodInfo method, Object[] parameters);
    }

    public class CallInterceptor<T> : ICallInterceptor
    {
        private readonly T _instance;
        private readonly IDataContext _dataContext;

        public CallInterceptor(T instance, IDataContext dataContext)
        {
            _instance = instance;
            _dataContext = dataContext;
        }

        public object Invoke(object proxy, MethodInfo method, object[] parameters)
        {
            return null;
        }
    }
}