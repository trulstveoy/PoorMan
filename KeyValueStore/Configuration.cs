using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using PoorMan.KeyValueStore.Annotation;

namespace PoorMan.KeyValueStore
{
    public class Configuration
    {
        private readonly string _connectionString;
        private readonly List<Type> _types = new List<Type>();

        private Func<Type, TypeDefinition> _getDefinition;
        
        public Configuration(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Configuration WithDocuments(params Type[] types)
        {
            var typeArray = types.ToArray();
            var definitions = typeArray.ToDictionary(type => type, type => CreateDefinition(type));
            _getDefinition = new Func<Type, TypeDefinition>(type =>
            {
                TypeDefinition typeDefinition;
                if (!definitions.TryGetValue(type, out typeDefinition))
                    throw new InvalidOperationException(
                        string.Format("No type definition exists for type {0}. Configure WithDocuments", type.FullName));

                return typeDefinition;
            });

            return this;
        }

        public IDataContext Create()
        {
            if(_getDefinition == null)
                throw new InvalidOperationException("Types not probed. Use WithDocuments.");
            return new DataContext(_connectionString, _getDefinition);
        }

        private TypeDefinition CreateDefinition(Type type)
        {
            return new TypeDefinition
            {
                Serializer = CreateSerializer(type),
                GetId = CreateGetId(type),
                Name = GetTypeName(type)
            };
        }

        private List<Type> GetDecendants(Type type, Type[] types)
        {
            return types.Where(t => t.IsAssignableFrom(type)).ToList();
        }

        private Func<object, object> CreateGetId(Type type)
        {
            var propertyInfo = type.GetProperties().FirstOrDefault(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(IdAttribute)));
            if (propertyInfo == null)
                throw new InvalidOperationException("Missing key attribute for document");
            if (!new[] { typeof(Guid), typeof(string), typeof(long), typeof(int) }.Contains(propertyInfo.PropertyType))
                throw new InvalidOperationException(string.Format("Id for type {0} has to be either Guid, string, long or int", type.FullName));

            return document =>
            {
                var id = propertyInfo.GetValue(document);
                if (id == null)
                {
                    throw new InvalidOperationException(string.Format("Key for type {0} cannot be null", type.FullName));
                }
                return id;
            };
        }

        private XmlSerializer CreateSerializer(Type type)
        {
            if (type.IsInterface)
                return null;

            var overrides = new XmlAttributeOverrides();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetSetMethod() == null || x.PropertyType.IsInterface))
            {
                if (propertyInfo.DeclaringType == null)
                    throw new InvalidOperationException(string.Format("Property {0} has no declaring type", propertyInfo.Name));
                overrides.Add(propertyInfo.DeclaringType, propertyInfo.Name, new XmlAttributes { XmlIgnore = true });
            }

            return new XmlSerializer(type, overrides);
        }

        private string GetTypeName(Type type)
        {
            return string.Format("{0}, {1}", type.FullName, type.Assembly.FullName.Split(',')[0]);
        }
    }

    public class TypeDefinition
    {
        public string Name { get; set; }
        public XmlSerializer Serializer { get; set; }
        public Func<object, object> GetId { get; set; }
    }
}
