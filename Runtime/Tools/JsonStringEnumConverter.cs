using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace Scribe.Tools
{

    public class StringEnum : StringEnumConverter
    {
        readonly object _nullValue;

        public StringEnum(object nullValue) => _nullValue = nullValue;

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            if (!enumType.IsEnum)
                throw new JsonSerializationException(
                    $"{nameof(StringEnum)} can only be used with enum types.");

            if (_nullValue == null || _nullValue.GetType() != enumType)
                throw new JsonSerializationException(
                    $"NullValue must be of type {enumType.Name}.");

            if (reader.TokenType == JsonToken.Null)
                return _nullValue;

            if (reader.TokenType == JsonToken.String &&
                string.IsNullOrWhiteSpace(reader.Value as string))
                return _nullValue;

            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException)
            {
                return _nullValue;
            }
        }
    }

}