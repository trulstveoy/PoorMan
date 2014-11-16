using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;

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
            }

            CustomAttributeData parentAttribute = invocation.Method.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ParentAttribute));
            if (parentAttribute != null)
            {
                object id = invocation.Arguments[0];
                var returnType = invocation.Method.ReturnType;

                var result = _dataContext.Read(id, returnType);
                invocation.ReturnValue = result;
                return;
            }

            invocation.ReturnValue = invocation.Method.Invoke(_instance, invocation.Arguments);
        }
    }
}
