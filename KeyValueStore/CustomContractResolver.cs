using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PoorMan.KeyValueStore
{
    public class CustomContractResolver : DefaultContractResolver
    {
        private readonly List<Type> _omittedAttributes = new List<Type>() {typeof(ChildAttribute)};
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            List<string> omittedPropertyNames 
                = type.GetProperties().Where(prop => prop.CustomAttributes.Count(attr => _omittedAttributes.Contains(attr.AttributeType)) > 0 ).Select(x => x.Name).ToList();

            var props = base.CreateProperties(type, memberSerialization);

            return props.Where(x => !omittedPropertyNames.Contains(x.PropertyName)).ToList();
        }
    }
}