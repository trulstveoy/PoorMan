using System;
using System.Collections.Generic;
using System.Linq;

namespace PoorMan.KeyValueStore
{
    public class Configuration
    {
        private readonly string _connectionString;
       
        private Dictionary<Type, TypeDefinition> _typeDefinitions;

        public Configuration(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Configuration WithDocuments(params Type[] types)
        {
            var typeArray = types.ToArray();
            _typeDefinitions = typeArray.Select(type => new
            {
                Type = type,
                Definition = CreateDefinition(type)
            }).ToDictionary(x => x.Type, y => y.Definition);

            return this;
        }

        public IDataContext Create()
        {
            if(_typeDefinitions == null)
                throw new InvalidOperationException("Types not probed. Use WithDocuments.");
            return new DataContext(_connectionString, _typeDefinitions);
        }

        public Configuration Output(Action<string, Dictionary<Type, TypeDefinition>> action)
        {
            action(_connectionString, _typeDefinitions);
            return this;
        }
        
        private TypeDefinition CreateDefinition(Type type)
        {
            return new TypeDefinition
            {
                Type = type,
                GetId = CreateGetId(type),
                Name = GetTypeName(type)
            };
        }

        private Func<object, object> CreateGetId(Type type)
        {
            var propertyInfo = type.GetProperties().FirstOrDefault(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(IdAttribute)));
            if (propertyInfo == null)
                throw new InvalidOperationException(string.Format("Missing Id attribute for document {0}", type.Name));
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

        private string GetTypeName(Type type)
        {
            return string.Format("{0}, {1}", type.FullName, type.Assembly.FullName.Split(',')[0]);
        }
    }

    public class TypeDefinition
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public Func<object, object> GetId { get; set; }
    }
}
