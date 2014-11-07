using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PoorMan.KeyValueStore.Annotation;

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
        private readonly object _id;

        public CallInterceptor(T instance, IDataContext dataContext, object id)
        {
            _instance = instance;
            _dataContext = dataContext;
            _id = id;
        }

        public object Invoke(object proxy, MethodInfo method, object[] parameters)
        {
            if (method.Name.StartsWith("get_"))
            {
                string propertyName = method.Name.Split('_').Last();

                var childAttribute = _instance.GetType().GetProperties()
                    .Where(x => x.Name == propertyName).SelectMany(x => x.CustomAttributes)
                    .FirstOrDefault(x => x.AttributeType == typeof (ChildAttribute));
                if (childAttribute != null)
                {
                    Type generic = method.ReturnType.GetGenericArguments().First();
                    var result = _dataContext.GetChildren(generic, _id);
                    return result;
                }

                CustomAttributeData parentIdAttribute = _instance.GetType().GetProperties()
                    .Where(x => x.Name == propertyName).SelectMany(x => x.CustomAttributes)
                    .FirstOrDefault(x => x.AttributeType == typeof(ParentIdAttribute));
                if (parentIdAttribute != null)
                {
                    string keyCol = (string)parentIdAttribute.ConstructorArguments.First().Value;
                    object parentId = _instance.GetType().GetProperties().FirstOrDefault(x => x.Name == keyCol).GetValue(_instance);
                    
                    Type returnType = method.ReturnType;
                    var result = _dataContext.Read(parentId, returnType);
                    return result;
                }
            }

            return method.Invoke(_instance, parameters);
        }
    }
}