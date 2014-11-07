using System;

namespace PoorMan.KeyValueStore.Annotation
{
    public class IdAttribute : Attribute
    {}

    public class ParentIdAttribute : Attribute
    {
        public string ParentIdProperty { get; private set; }
        public ParentIdAttribute(string parentIdProperty)
        {
            ParentIdProperty = parentIdProperty;
        }
    }
}