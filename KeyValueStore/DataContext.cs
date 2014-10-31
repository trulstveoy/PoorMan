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

namespace PoorMan.KeyValueStore
{
    public class DataContext
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

        private T Deserialize<T>(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof (T), CreateOverrides(typeof(T)));
            return (T)serializer.Deserialize(reader);
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
                    command.Parameters.AddWithValue("@type", document.GetType().FullName);
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
                    command.Parameters.AddWithValue("@type", document.GetType().FullName);
                    command.ExecuteNonQuery();
                }
            ));
        }

        public T Read<T>(object id)
        {
            ValidateId(id);
            var result = SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE Id = @id AND type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", typeof(T).FullName);
                var reader = command.ExecuteReader();
                if (!reader.Read())
                    return null;
                
                return new
                {
                    Value = reader.GetXmlReader(reader.GetOrdinal("Value")),
                    Type = reader.GetString(reader.GetOrdinal("Type"))
                };
            });

            if (result == null)
                return default(T);
           
            return Deserialize<T>(result.Value);
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
                command.Parameters.AddWithValue("@parentType", typeof(TP).FullName);
                command.Parameters.AddWithValue("@child", childId);
                command.Parameters.AddWithValue("@childType", typeof(TC).FullName);
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
                command.Parameters.AddWithValue("@parentType", typeof(TP).FullName);
                command.Parameters.AddWithValue("@child", childId);
                command.Parameters.AddWithValue("@childType", typeof(TC).FullName);
                command.ExecuteNonQuery();
            });
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

            return result.Select(x => Deserialize<T>(x.Value)).ToList();
        }

        public List<T> ReadAll<T>()
        {
            var result = SqlQuery(command =>
            {
                command.CommandText = "SELECT Value, Type FROM KeyValueStore WHERE type = @type";
                command.Parameters.AddWithValue("@type", typeof(T).FullName);
                var reader = command.ExecuteReader();

                var values = new List<XmlReader>();
                while (reader.Read())
                {
                    values.Add(reader.GetXmlReader(reader.GetOrdinal("Value")));                        
                }

                return values;
            });

            return result.Select(Deserialize<T>).ToList();
        }

        public void Delete<T>(object id)
        {
            ValidateId(id);
            
            SqlAction(command =>
            {
                command.CommandText = "DELETE FROM KeyValueStore WHERE Id = @id and Type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@type", typeof(T).FullName);
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
    }
}
