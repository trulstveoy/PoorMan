using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Xml;
using PoorMan.KeyValueStore.Interception;

namespace PoorMan.KeyValueStore
{
    public interface IDataContext
    {
        void EnsureNewDatabase();
        void Create<T>(T document) where T : class;
        void Update<T>(T document) where T : class;
        T Read<T>(object id);
        T ReadWithRelations<T>(object id);
        object Read(object id, Type type);
        object ReadWithRelations(object id, Type type);
        void AppendChild<TP, TC>(TP parent, TC child);
        void RemoveChild<TP, TC>(TP parent, TC child);
        List<TC> GetChildren<TP, TC>(TP document);
        List<object> GetChildren(Type childType, object parentId);
        List<T> ReadAll<T>();
        void Delete<T>(object id);
    }

    internal class DataContext : IDataContext
    {
        private readonly string _connectionstring;
        private readonly Func<Type, TypeDefinition> _getDefinition;
        private readonly Func<Type, TypeDefinition> _getProxyDefinition;

        public DataContext(string connectionstring, Func<Type, TypeDefinition> getDefinition, Func<Type, TypeDefinition> getProxyDefinition)
        {
            _connectionstring = connectionstring;
            _getDefinition = getDefinition;
            _getProxyDefinition = getProxyDefinition;
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
            if (id == null || !validTypes.Contains(id.GetType()))
            {
                throw new InvalidOperationException("Id needs to be of type Guid, string, long or int and cannot be null");
            }
        }

        private void ValidateDocument(params object[] documents)
        {
            if (documents.Any(document => document == null))
            {
                throw new InvalidOperationException("Document cannot be null");
            }
        }

        private void Serialize(object obj, Action<SqlXml> action)
        {
            using (var stream = new MemoryStream())
            {
                _getDefinition(obj.GetType()).Serializer.Serialize(stream, obj);
                action(new SqlXml(stream));
            }
        }

        private object Deserialize(XmlReader reader, Type type)
        {
            return _getDefinition(type).Serializer.Deserialize(reader);
        }
        
        public void Create<T>(T document) where T : class
        {
            ValidateDocument(document);
            var id = _getDefinition(document.GetType()).GetId(document);

            SqlAction(command => 
                Serialize(document, sqlXml => 
                {
                    command.CommandText = "INSERT INTO KeyValueStore (Id, Value, Type, LastUpdated) VALUES(@id, @value, @type, SYSDATETIME())";
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = sqlXml;
                    command.Parameters.AddWithValue("@type", _getDefinition(document.GetType()).Name);
                    command.ExecuteNonQuery();
                }));
        }

        public void Update<T>(T document) where T : class
        {
            ValidateDocument(document);
            var id = _getDefinition(document.GetType()).GetId(document);

            SqlAction(command =>
                Serialize(document, sqlXml =>
                {
                    command.CommandText = "UPDATE KeyValueStore SET Value = @value, Type = @type, LastUpdated = SYSDATETIME() WHERE Id = @id AND type = @type";
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = sqlXml;
                    command.Parameters.AddWithValue("@type", _getDefinition(document.GetType()).Name);
                    command.ExecuteNonQuery();
                }
            ));
        }

        private Tuple<XmlReader, string> ReadParent<T>(object id)
        {
            var decendants = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes().Where(type =>
                !type.IsInterface && !type.IsAbstract && typeof (T).IsAssignableFrom(type) && !type.Name.EndsWith("Proxy"))).Select(x => _getDefinition(x).Name).ToList();
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

        private Tuple<XmlReader, string> ReadConcrete(object id, Type type)
        {
            return SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE Id = @id AND type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", _getDefinition(type).Name);
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

            var result = typeof (T).IsInterface ? ReadParent<T>(id) : ReadConcrete(id, typeof(T));

            if (result == null)
                return default(T);
           
            return (T)Deserialize(result.Item1, Type.GetType(result.Item2));
        }

        public object Read(object id, Type type)
        {
            ValidateId(id);

            var result = ReadConcrete(id, type);

            if (result == null)
                return null;

            return Deserialize(result.Item1, type);
        }

        public T ReadWithRelations<T>(object id)
        {
            T instance = Read<T>(id);
            if (instance == null)
                return default(T);

            var proxyType = _getProxyDefinition(typeof (T)).Type;
            var proxy = Activator.CreateInstance(proxyType);
            ((IInterceptorSetter)proxy).SetInterceptor(new CallInterceptor(instance, this, id));

            return (T)proxy;
        }

        public object ReadWithRelations(object id, Type type)
        {
            object instance = Read(id, type);
            if (instance == null)
                return null;

            var proxy = new ProxyFactory().Create(type);
            ((IInterceptorSetter)proxy).SetInterceptor(new CallInterceptor(instance, this, id));

            return proxy;
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

        public void AppendChild<TP, TC>(TP parent, TC child)
        {
            ValidateDocument(parent, child);
            var parentDef = _getDefinition(parent.GetType());
            var childDef = _getDefinition(child.GetType());
            
            SqlAction(command =>
            {
                command.CommandText = "INSERT INTO Relation (Parent, ParentType, Child, ChildType, LastUpdated) VALUES(@parent, @parentType, @child, @childType, SYSDATETIME())";
                command.Parameters.AddWithValue("@parent", parentDef.GetId(parent));
                command.Parameters.AddWithValue("@parentType", parentDef.Name);
                command.Parameters.AddWithValue("@child", childDef.GetId(child));
                command.Parameters.AddWithValue("@childType", childDef.Name);
                command.ExecuteNonQuery();
            });
        }

        public void RemoveChild<TP, TC>(TP parent, TC child)
        {
            ValidateDocument(parent, child);
            var parentDef = _getDefinition(parent.GetType());
            var childDef = _getDefinition(child.GetType());
            
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM Relation WHERE Parent = @parent AND ParentType = @parentType AND Child = @child AND ChildType = @childType";
                command.Parameters.AddWithValue("@parent", parentDef.GetId(parent));
                command.Parameters.AddWithValue("@parentType", parentDef.Name);
                command.Parameters.AddWithValue("@child", childDef.GetId(child));
                command.Parameters.AddWithValue("@childType", childDef.Name);
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

        public List<TC> GetChildren<TP, TC>(TP parent)
        {
            const string query = @"SELECT Value, Type FROM KeyValueStore k
                                  JOIN Relation r on r.Child = k.Id 
                                  AND r.Parent = @parent";

            var result = SqlQuery(command =>
            {
                command.CommandText = query;
                command.Parameters.AddWithValue("@parent", _getDefinition(parent.GetType()).GetId(parent));
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

            return result.Select(x => (TC)Deserialize(x.Value, Type.GetType(x.Type))).ToList();
        }

        public List<T> ReadAll<T>()
        {
            var result = SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE type = @type";
                command.Parameters.AddWithValue("@type", _getDefinition(typeof(T)).Name);
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

            return result.Select(x => (T)Deserialize(x.Item1, Type.GetType(x.Item2))).ToList();
        }

        public void Delete<T>(object id)
        {
            ValidateId(id);
            
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM KeyValueStore WHERE Id = @id and Type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", _getDefinition(typeof(T)).Name);
                command.ExecuteNonQuery();
            });
        }
    }
}
