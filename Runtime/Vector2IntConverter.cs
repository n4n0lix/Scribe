using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Scribe
{
    public class Vector2IntConverter : JsonConverter<Vector2Int>
    {
        public override void WriteJson(JsonWriter writer, Vector2Int value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2Int ReadJson(JsonReader reader, Type objectType, Vector2Int existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            int x = 0, y = 0;

            if (reader.TokenType == JsonToken.Null)
                return default;

            while (reader.Read())
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = (string)reader.Value;
                    if (!reader.Read()) continue;

                    switch (propertyName)
                    {
                        case "x":
                            x = Convert.ToInt32(reader.Value);
                            break;
                        case "y":
                            y = Convert.ToInt32(reader.Value);
                            break;
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    return new Vector2Int(x, y);
                }

            throw new JsonSerializationException("Unexpected end when reading Vector2Int.");
        }
    }
}