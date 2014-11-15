using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using PoorMan.KeyValueStore.Annotation;

namespace PoorMan.KeyValueStore
{
    public class CallInterceptor : IInterceptor
    {
        private readonly object _instance;
        private readonly IDataContext _dataContext;
        private readonly object _id;

        public CallInterceptor(object instance, IDataContext dataContext, object id)
        {
            _instance = instance;
            _dataContext = dataContext;
            _id = id;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name == "GetInstance")
            {
                invocation.ReturnValue = _instance;
                return;
            }

            if (invocation.Method.Name.StartsWith("get_"))
            {
                string propertyName = invocation.Method.Name.Split('_').Last();
                var childAttribute = _instance.GetType().GetProperties()
                    .Where(x => x.Name == propertyName).SelectMany(x => x.CustomAttributes)
                    .FirstOrDefault(x => x.AttributeType == typeof(ChildAttribute));
                if (childAttribute != null)
                {
                    Type generic = invocation.Method.ReturnType.GetGenericArguments().First();
                    var items = _dataContext.GetChildren(generic, _id).ToList();

                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(generic));
                    foreach (var item in items)
                        list.Add(item);

                    invocation.ReturnValue = list;
                    return;
                }

                CustomAttributeData parentIdAttribute = _instance.GetType().GetProperties()
                    .Where(x => x.Name == propertyName).SelectMany(x => x.CustomAttributes)
                    .FirstOrDefault(x => x.AttributeType == typeof(ParentIdAttribute));
                if (parentIdAttribute != null)
                {
                    string keyCol = (string)parentIdAttribute.ConstructorArguments.First().Value;
                    object parentId = _instance.GetType().GetProperties().FirstOrDefault(x => x.Name == keyCol).GetValue(_instance);

                    Type returnType = invocation.Method.ReturnType;
                    var result = _dataContext.Read(parentId, returnType);
                    invocation.ReturnValue = result;
                    return;
                }
            }

            invocation.ReturnValue = invocation.Method.Invoke(_instance, invocation.Arguments);
        }
    }
}
