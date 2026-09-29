using Newtonsoft.Json;

namespace Scribe
{
    public static class JsonConfig
    {
        static JsonConfig()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new Vector2IntConverter() },
            };
        }

        // Call this early in your app, e.g. from a static initializer, GameManager, or before any serialization happens
        public static void Initialize()
        {
        }
    }
}