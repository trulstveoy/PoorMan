using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PoorMan.KeyValueStore
{
    public class Configuration
    {
        private string _connectionString;
        private readonly List<Type> _types = new List<Type>();

        public Configuration(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Configuration WithTypes(params Type[] types)
        {
            _types.AddRange(types);
            return this;
        }

        public Configuration AssembliesToPrope(params Assembly[] assemblies)
        {
            _types.AddRange(assemblies.SelectMany(x => x.GetTypes()));
            return this;
        }

        public IDataContext Create()
        {
            return new DataContext(_connectionString);
        }
    }
}
