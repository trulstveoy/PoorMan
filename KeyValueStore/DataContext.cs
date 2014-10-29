using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace PoorMan.KeyValueStore
{
    public class DataContext
    {
        private readonly Settings _settings = new Settings();
        private readonly string _connectionstring;

        public DataContext(string connectionstring)
        {
            _connectionstring = connectionstring;
        }

        public Settings Settings { get { return _settings; }  }
        
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

        public void Create<T>(object id, T document)
        {
            ValidateId(id);
            var json = JsonConvert.SerializeObject(document, Settings.JsonSerializerSettings);

            SqlAction(command =>
            {
                command.CommandText = "INSERT INTO KeyValueStore (Id, Value, Type, LastUpdated) VALUES(@id, @value, @type, GETDATE())";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@value", json);
                command.Parameters.AddWithValue("@type", typeof(T).FullName);
                command.ExecuteNonQuery();
            });
        }

        public void Update<T>(object id, T document)
        {
            ValidateId(id);
            var json = JsonConvert.SerializeObject(document, Settings.JsonSerializerSettings);

            SqlAction(command =>
            {
                command.CommandText = "UPDATE KeyValueStore SET Value = @value, Type = @type, LastUpdated = GETDATE() WHERE Id = @id AND type = @type";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@value", json);
                command.Parameters.AddWithValue("@type", typeof(T).FullName);
                command.ExecuteNonQuery();
            });
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
                    Value = reader.GetString(reader.GetOrdinal("Value")),
                    Type = reader.GetString(reader.GetOrdinal("Type"))
                };
            });

            if (result == null)
                return default(T);
           
            return JsonConvert.DeserializeObject<T>(result.Value);
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
                command.CommandText = "INSERT INTO Relation (Parent, ParentType, Child, ChildType, LastUpdated) VALUES(@parent, @parentType, @child, @childType, GETDATE())";
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

            return result.Select(x => JsonConvert.DeserializeObject<T>(x.Value)).ToList();
        }
    }
}
