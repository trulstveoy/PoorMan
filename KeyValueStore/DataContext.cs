using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Castle.DynamicProxy;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace PoorMan.KeyValueStore
{
    public interface IDataContext
    {
        void EnsureNewDatabase();
        void Insert<T>(T document) where T : class;
        void Update<T>(T document) where T : class;
        void Upsert<T>(T document) where T : class;
        T Read<T>(object id) where T : class;
        object Read(object id, Type type);
        void AppendChild<TP, TC>(TP parent, TC child);
        void RemoveChild<TP, TC>(TP parent, TC child);
        List<TC> GetChildren<TP, TC>(TP document);
        List<object> GetChildren(Type childType, object parentId);
        List<T> ReadAll<T>();
        void Delete<T>(object id) where T : class;
    }

    internal class DataContext : IDataContext
    {
        private static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();

        private readonly string _connectionstring;
        private readonly Dictionary<Type, TypeDefinition> _typeDefinitions;

        public DataContext(string connectionstring, Dictionary<Type, TypeDefinition> typeDefinitions)
        {
            _connectionstring = connectionstring;
            _typeDefinitions = typeDefinitions;
        }

        private TypeDefinition GetDefinition(Type type)
        {
            TypeDefinition typeDefinition;
            if (!_typeDefinitions.TryGetValue(type, out typeDefinition))
                throw new InvalidOperationException(
                    string.Format("No type definition exists for type {0}. Configure WithDocuments", type.FullName));

            return typeDefinition;
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

        private string Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var xDoc = JsonConvert.DeserializeXNode(json, "root", true);
            return xDoc.ToString();
        }

        private object Deserialize(string str, Type type)
        {
            var xDocument = XDocument.Parse(str);
            var json = JsonConvert.SerializeXNode(xDocument, Formatting.None, true);
            return JsonConvert.DeserializeObject(json, type);
        }
        
        public void Insert<T>(T document) where T : class
        {
            ValidateDocument(document);
            var id = GetDefinition(document.GetType()).GetId(document);

            SqlAction(command => 
                {
                    command.CommandText = "INSERT INTO KeyValueStore (Id, Value, Type, LastUpdated) VALUES(@id, @value, @type, SYSDATETIME())";
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = Serialize(document);
                    command.Parameters.AddWithValue("@type", GetDefinition(document.GetType()).Name);
                    command.ExecuteNonQuery();
                });
        }

        public void Update<T>(T document) where T : class
        {
            ValidateDocument(document);

            var instance = GetInstance(document);

            var id = GetDefinition(instance.GetType()).GetId(instance);

            SqlAction(command =>
                {
                    command.CommandText = "UPDATE KeyValueStore SET Value = @value, Type = @type, LastUpdated = SYSDATETIME() WHERE Id = @id AND type = @type";
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = Serialize(instance);
                    command.Parameters.AddWithValue("@type", GetDefinition(instance.GetType()).Name);
                    command.ExecuteNonQuery();
                }
            );
        }

        public void Upsert<T>(T document) where T : class
        {
            ValidateDocument(document);

            var instance = GetInstance(document);

            var id = GetDefinition(instance.GetType()).GetId(instance);

            SqlAction(command =>
                {
                    const string sql = "MERGE INTO KeyValueStore AS target USING (VALUES(@id, @value, @type, SYSDATETIME())) AS source (Id, Value, Type, LastUpdated) " +
                                       "ON source.Id = target.Id AND source.Type = target.Type " +
                                       "WHEN MATCHED THEN UPDATE SET Value = source.Value " +
                                       "WHEN NOT MATCHED THEN INSERT (Id, Value, Type, LastUpdated) VALUES(source.Id, source.Value, source.Type, source.LastUpdated);";
                    command.CommandText = sql;
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.Add("@value", SqlDbType.Xml).Value = Serialize(instance);
                    command.Parameters.AddWithValue("@type", GetDefinition(instance.GetType()).Name);
                    command.ExecuteNonQuery();
                }
            );
        }

        private static object GetInstance(object document)
        {
            object instance = document;
            var proxy = document as IProxy;
            if (proxy != null)
            {
                instance = proxy.GetInstance();
            }
            return instance;
        }

        public T Read<T>(object id) where T : class
        {
            var result = Read(id, typeof (T));
            if (result == null)
                return default(T);

            return (T) result;
        }

        public object Read(object id, Type type)
        {
            ValidateId(id);

            var result = SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE Id = @id AND type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", GetDefinition(type).Name);
                var reader = command.ExecuteReader();
                if (!reader.Read())
                    return null;

                return new Tuple<string, string>(
                    reader.GetString(reader.GetOrdinal("Value")),
                    reader.GetString(reader.GetOrdinal("Type")));
            });

            if (result == null)
                return null;

            var instance = Deserialize(result.Item1, type);
            return ProxyGenerator.CreateClassProxy(type, new[] { typeof(IProxy) }, new CallInterceptor(instance, this, id));
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

            var parentInstance = GetInstance(parent);
            var childInstance = GetInstance(child);

            var parentDef = GetDefinition(parentInstance.GetType());
            var childDef = GetDefinition(childInstance.GetType());
            
            SqlAction(command =>
            {
                command.CommandText = "INSERT INTO Relation (Parent, ParentType, Child, ChildType, LastUpdated) VALUES(@parent, @parentType, @child, @childType, SYSDATETIME())";
                command.Parameters.AddWithValue("@parent", parentDef.GetId(parentInstance));
                command.Parameters.AddWithValue("@parentType", parentDef.Name);
                command.Parameters.AddWithValue("@child", childDef.GetId(childInstance));
                command.Parameters.AddWithValue("@childType", childDef.Name);
                command.ExecuteNonQuery();
            });
        }

        public void RemoveChild<TP, TC>(TP parent, TC child)
        {
            ValidateDocument(parent, child);

            var parentInstance = GetInstance(parent);
            var childInstance = GetInstance(child);

            var parentDef = GetDefinition(parentInstance.GetType());
            var childDef = GetDefinition(childInstance.GetType());
            
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM Relation WHERE Parent = @parent AND ParentType = @parentType AND Child = @child AND ChildType = @childType";
                command.Parameters.AddWithValue("@parent", parentDef.GetId(parentInstance));
                command.Parameters.AddWithValue("@parentType", parentDef.Name);
                command.Parameters.AddWithValue("@child", childDef.GetId(childInstance));
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

                var list = new List<Tuple<string, string>>();
                while (reader.Read())
                {
                    list.Add(new Tuple<string, string>(reader.GetString(reader.GetOrdinal("Value")), reader.GetString(reader.GetOrdinal("Type"))));
                }

                return list.Select(x => new
                {
                    Value = x.Item1,
                    Type = x.Item2
                }).ToList();
            });

            var retVal = result.Select(x =>
            {
                var type = Type.GetType(x.Type);
                var instance = Deserialize(x.Value, type);
                return ProxyGenerator.CreateClassProxy(type, new[] { typeof(IProxy) }, new CallInterceptor(instance, this, GetDefinition(type).GetId(instance)));
            }).ToList();
            return retVal;
        }

        public List<TC> GetChildren<TP, TC>(TP parent)
        {
            var parentInstance = GetInstance(parent);
            var id = GetDefinition(parentInstance.GetType()).GetId(parent);
            var result = GetChildren(typeof (TC), id);
            return result.Select(x => (TC) x).ToList();
        }

        public List<T> ReadAll<T>()
        {
            var result = SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE type = @type";
                command.Parameters.AddWithValue("@type", GetDefinition(typeof(T)).Name);
                var reader = command.ExecuteReader();

                var values = new List<Tuple<string, string>>();
                while (reader.Read())
                {
                    values.Add(new Tuple<string, string>(
                        reader.GetString(reader.GetOrdinal("Value")),
                        reader.GetString(reader.GetOrdinal("Type"))));
                }

                return values;
            });

            return result.Select(x => (T)Deserialize(x.Item1, Type.GetType(x.Item2))).ToList();
        }

        public void Delete<T>(object id) where T : class
        {
            ValidateId(id);
            
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM KeyValueStore WHERE Id = @id and Type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", GetDefinition(typeof(T)).Name);
                command.ExecuteNonQuery();
            });
        }
    }
}
