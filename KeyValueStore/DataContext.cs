using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using PoorMan.KeyValueStore.Interception;

namespace PoorMan.KeyValueStore
{
    public interface IDataContext
    {
        void EnsureNewDatabase();
        void Create<T>(object id, T document);
        void Update<T>(object id, T document);
        T Read<T>(object id);
        void AppendChild<TP, TC>(object parentId, object childId);
        void RemoveChild<TP, TC>(object parentId, object childId);
        List<T> GetChildren<T>(object parentId);
        List<object> GetChildren(Type childType, object parentId);
        List<T> ReadAll<T>();

        void Delete<T>(object id);
        T Read<T>(object id, Type type);
        T ReadWithChildren<T>(object id);
    }

    internal class DataContext : IDataContext
    {
        private readonly string _connectionstring;
       
        public DataContext(string connectionstring)
        {
            _connectionstring = connectionstring;
        }
        
        public void EnsureNewDatabase()
        {
            var script = new Func<string, string>(name =>
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream(name))
                // ReSharper disable once AssignNullToNotNullAttribute
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            })("PoorMan.KeyValueStore.Scripts.Sql.KeyValueStore.sql");
            
            using (var connection = new SqlConnection(_connectionstring))
            {
                connection.Open();
                using (var command = connection.CreateCommand())        
                {
                    command.CommandText = script;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void ValidateId(object id)
        {
            var validTypes = new[] {typeof (Guid), typeof (string), typeof (long), typeof (int)};
            if (!validTypes.Contains(id.GetType()))
            {
                throw new InvalidOperationException("Id needs to be of type Guid, string, long or int");
            }
        }

        private void ValidateDocument<T>(T document)
        {
            if(document == null)
                throw new InvalidOperationException("Document cannot be null");
        }

        private void Serialize<T>(T obj, Action<SqlXml> action)
        {
            var serializer = new XmlSerializer(obj.GetType(), CreateOverrides(typeof(T)));
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, obj);
                action(new SqlXml(stream));
            }
        }

        private T Deserialize<T>(XmlReader reader, Type persistedType)
        {
            var serializedType = persistedType ?? typeof (T);
            var serializer = new XmlSerializer(serializedType, CreateOverrides(serializedType));
            return (T)serializer.Deserialize(reader);
        }

        private object Deserialize(XmlReader reader, Type type)
        {
            var serializer = new XmlSerializer(type, CreateOverrides(type));
            return serializer.Deserialize(reader);
        }

        public void Create<T>(object id, T document)
        {
            ValidateId(id);
            ValidateDocument(document);
            
            SqlAction(command => 
                Serialize(document, sqlXml => 
                {
                    command.CommandText = "INSERT INTO KeyValueStore (Id, Value, Type, LastUpdated) VALUES(@id, @value, @type, SYSDATETIME())";
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = sqlXml;
                    command.Parameters.AddWithValue("@type", GetName(document.GetType()));
                    command.ExecuteNonQuery();
                }));
        }

        public void Update<T>(object id, T document)
        {
            ValidateId(id);
            ValidateDocument(document);

            SqlAction(command =>
                Serialize(document, sqlXml =>
                {
                    command.CommandText = "UPDATE KeyValueStore SET Value = @value, Type = @type, LastUpdated = SYSDATETIME() WHERE Id = @id AND type = @type";
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = sqlXml;
                    command.Parameters.AddWithValue("@type", GetName(document.GetType()));
                    command.ExecuteNonQuery();
                }
            ));
        }

        private Tuple<XmlReader, string> ReadParent<T>(object id)
        {
            var decendants = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes().Where(type =>
                !type.IsInterface && !type.IsAbstract && typeof (T).IsAssignableFrom(type))).Select(GetName).ToList();
            var inClause = string.Join(",", Enumerable.Range(0, decendants.Count()).Select(x => string.Format("@{0}", x)));

            return SqlQuery(command =>
            {
                command.CommandText = string.Format("SELECT Value, Type FROM KeyValueStore WHERE Id = @id AND type IN ({0})", inClause);
                command.Parameters.AddWithValue("@id", id);
                for (int i = 0; i < decendants.Count; i++)
                    command.Parameters.AddWithValue("@" + i, decendants[i]);
                var reader = command.ExecuteReader();
                if (!reader.Read())
                    return null;

                return new Tuple<XmlReader, string>(
                    reader.GetXmlReader(reader.GetOrdinal("Value")),
                    reader.GetString(reader.GetOrdinal("Type")));
            });
        }

        private Tuple<XmlReader, string> ReadConcrete<T>(object id)
        {
            return SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE Id = @id AND type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", GetName(typeof(T)));
                var reader = command.ExecuteReader();
                if (!reader.Read())
                    return null;

                return new Tuple<XmlReader, string>(
                    reader.GetXmlReader(reader.GetOrdinal("Value")),
                    reader.GetString(reader.GetOrdinal("Type")));
            });
        }

        public T Read<T>(object id)
        {
            ValidateId(id);

            var result = typeof (T).IsInterface ? ReadParent<T>(id) : ReadConcrete<T>(id);

            if (result == null)
                return default(T);
           
            return Deserialize<T>(result.Item1, Type.GetType(result.Item2));
        }

        public T ReadWithChildren<T>(object id)
        {
            var instance = Read<T>(id);
            if (instance == null)
                return default(T);
            
            var proxy = new ProxyFactory().Create<T>();
            ((IInterceptorSetter)proxy).SetInterceptor(new CallInterceptor<T>(instance, this, id));

            return proxy;
        }

        public T Read<T>(object id, Type type)
        {
            ValidateId(id);
            
            var result = typeof(T).IsInterface ? ReadParent<T>(id) : ReadConcrete<T>(id);

            if (result == null)
                return default(T);

            return Deserialize<T>(result.Item1, type);
        }

        private T SqlQuery<T>(Func<SqlCommand, T> func)
        {
            using (var connection = new SqlConnection(_connectionstring))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    return func(command);
                }
            }
        }

        private void SqlAction(Action<SqlCommand> action)
        {
            using (var connection = new SqlConnection(_connectionstring))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    action(command);
                }
            }
        }

        public void AppendChild<TP, TC>(object parentId, object childId)
        {
            ValidateId(parentId);
            ValidateId(childId);
            SqlAction(command =>
            {
                command.CommandText = "INSERT INTO Relation (Parent, ParentType, Child, ChildType, LastUpdated) VALUES(@parent, @parentType, @child, @childType, SYSDATETIME())";
                command.Parameters.AddWithValue("@parent", parentId);
                command.Parameters.AddWithValue("@parentType", typeof(TP).AssemblyQualifiedName);
                command.Parameters.AddWithValue("@child", childId);
                command.Parameters.AddWithValue("@childType", typeof(TC).AssemblyQualifiedName);
                command.ExecuteNonQuery();
            });
        }

        public void RemoveChild<TP, TC>(object parentId, object childId)
        {
            ValidateId(parentId);
            ValidateId(childId);
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM Relation WHERE Parent = @parent AND ParentType = @parentType AND Child = @child AND ChildType = @childType";
                command.Parameters.AddWithValue("@parent", parentId);
                command.Parameters.AddWithValue("@parentType", typeof(TP).AssemblyQualifiedName);
                command.Parameters.AddWithValue("@child", childId);
                command.Parameters.AddWithValue("@childType", typeof(TC).AssemblyQualifiedName);
                command.ExecuteNonQuery();
            });
        }

        public List<object> GetChildren(Type childType, object parentId)
        {
            ValidateId(parentId);
            const string query = @"SELECT Value, Type FROM KeyValueStore k
                                  JOIN Relation r on r.Child = k.Id 
                                  AND r.Parent = @parent";

            var result = SqlQuery(command =>
            {
                command.CommandText = query;
                command.Parameters.AddWithValue("@parent", parentId);
                var reader = command.ExecuteReader();

                var list = new List<Tuple<XmlReader, string>>();
                while (reader.Read())
                {
                    list.Add(new Tuple<XmlReader, string>(reader.GetXmlReader(reader.GetOrdinal("Value")), reader.GetString(reader.GetOrdinal("Type"))));
                }

                return list.Select(x => new
                {
                    Value = x.Item1,
                    Type = x.Item2
                }).ToList();
            });

            return result.Select(x => Deserialize(x.Value, Type.GetType(x.Type))).ToList();
        }

        public List<T> GetChildren<T>(object parentId)
        {
            ValidateId(parentId);
            const string query = @"SELECT Value, Type FROM KeyValueStore k
                                  JOIN Relation r on r.Child = k.Id 
                                  AND r.Parent = @parent";

            var result = SqlQuery(command =>
            {
                command.CommandText = query;
                command.Parameters.AddWithValue("@parent", parentId);
                var reader = command.ExecuteReader();

                var list = new List<Tuple<XmlReader, string>>();
                while (reader.Read())
                {
                    list.Add(new Tuple<XmlReader, string>(reader.GetXmlReader(reader.GetOrdinal("Value")), reader.GetString(reader.GetOrdinal("Type"))));
                }

                return list.Select(x => new
                {
                    Value = x.Item1,
                    Type = x.Item2
                }).ToList();
            });

            return result.Select(x => Deserialize<T>(x.Value, Type.GetType(x.Type))).ToList();
        }

        public List<T> ReadAll<T>()
        {
            var result = SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE type = @type";
                command.Parameters.AddWithValue("@type", GetName(typeof(T)));
                var reader = command.ExecuteReader();

                var values = new List<Tuple<XmlReader, string>>();
                while (reader.Read())
                {
                    values.Add(new Tuple<XmlReader, string>(
                        reader.GetXmlReader(reader.GetOrdinal("Value")),
                        reader.GetString(reader.GetOrdinal("Type"))));
                }

                return values;
            });

            return result.Select(x => Deserialize<T>(x.Item1, Type.GetType(x.Item2))).ToList();
        }

        public void Delete<T>(object id)
        {
            ValidateId(id);
            
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM KeyValueStore WHERE Id = @id and Type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", GetName(typeof(T)));
                command.ExecuteNonQuery();
            });
        }

        private XmlAttributeOverrides CreateOverrides(Type type)
        {
            var overrides = new XmlAttributeOverrides();

            foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetSetMethod() == null || x.PropertyType.IsInterface))
            {
                if (propertyInfo.DeclaringType == null)
                    throw new InvalidOperationException(string.Format("Property {0} has no declaring type", propertyInfo.Name));
                overrides.Add(propertyInfo.DeclaringType, propertyInfo.Name, new XmlAttributes { XmlIgnore = true });
            }

            return overrides;
        }

        private string GetName(Type type)
        {
            return string.Format("{0}, {1}", type.FullName, type.Assembly.FullName.Split(',')[0]);
        }
    }
}
