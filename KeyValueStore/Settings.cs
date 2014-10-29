using Newtonsoft.Json;

namespace PoorMan.KeyValueStore
{
    public class Settings
    {
        private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings();
        public JsonSerializerSettings JsonSerializerSettings
        {
            get { return _jsonSerializerSettings; }
            set { _jsonSerializerSettings = value; }
        }
    }
}
