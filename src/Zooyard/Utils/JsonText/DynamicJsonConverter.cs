using System.Text.Json.Serialization;

namespace System.Text.Json
{
    // Adds support for adding to ICollection<T> properties that are read-only
    // https://github.com/dotnet/runtime/issues/30258
    public sealed class DynamicJsonConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(object);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            => new DynamicJsonElementConverter();

        private class DynamicJsonElementConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => DynamicJsonElement.From(JsonSerializer.Deserialize<JsonElement>(ref reader, options));

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
                => throw new NotSupportedException();
        }
    }
}