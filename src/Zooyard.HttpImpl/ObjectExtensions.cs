using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zooyard.HttpImpl;

internal static class ObjectExtensions
{
    public readonly static JsonSerializerOptions _option = new()
    {
        // 解决中文序列化被编码的问题
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // 解决属性名称大小写敏感问题 
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = {
            new IntConverter(),
            new IntNullableConverter(),
            new LongConverter(),
            new LongNullableConverter(),
            new DateTimeConverter(),
            new DateTimeNullableConverter(),
            new BoolConverter(),
            new BoolNullableConverter(),
            new GuidConverter(),
            new GuidNullableConverter(),
            new DecimalConverter(),
            new DecimalNullableConverter(),
            new DictionaryLongStringJsonConverter(),
            new JsonStringEnumConverter(),
            new StringConverter()
        }
    };
    /// <summary>
    /// Convert an object to a JSON string with camelCase formatting
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="empty"></param>
    /// <returns></returns>
    public static string ToJsonString(this object obj, string empty = "")
    {
        if (obj == null)
        {
            return empty;
        }

        var result = JsonSerializer.Serialize(obj, _option);

        return result;
    }
    /// <summary>
    /// Deserializes the json.
    /// </summary>
    /// <param name="str">The STR.</param>
    /// <param name="defaultValue">The STR.</param>
    /// <returns></returns>
    public static T DeserializeJson<T>(this string str, T defaultValue = default!)
    {
        if (!string.IsNullOrEmpty(str))
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(str, _option);

                return result!;
            }
            catch (Exception)
            {
                return defaultValue;
            }

        }
        return defaultValue;
    }

    /// <summary>
    /// Deserializes the json.
    /// </summary>
    /// <param name="str">The STR.</param>
    /// <param name="returnType">The returnType.</param>
    /// <returns></returns>
    public static object DeserializeJson(this string str, Type returnType)
    {
        if (!string.IsNullOrEmpty(str))
        {
            try
            {
                var result = JsonSerializer.Deserialize(str, returnType, _option);
                return result!;
            }
            catch (Exception)
            {
                return default!;
            }

        }
        return default!;
    }

    public static async Task<T> DeserializeJsonAsync<T>(this Stream utf8stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (utf8stream == null)
        {
            throw new Exception(" utf8stream is null");
        }
        var result = await JsonSerializer.DeserializeAsync<T>(utf8stream, _option, cancellationToken);
        return result!;
    }
}

internal class BoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return false;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return false;
            }

            if (bool.TryParse(readStr, out bool val))
            {
                return val;
            }
        }

        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }
        if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }

        return reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
internal class BoolNullableConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) 
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return null;
            }

            if (bool.TryParse(readStr, out bool val))
            {
                return val;
            }
        }

        if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }
        if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }

        return reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteStringValue("");
        }
        else {
            writer.WriteBooleanValue(value.Value);
        }
    }
}
internal class DecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return 0;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return 0;
            }

            if (decimal.TryParse(readStr, out decimal val))
            {
                return val;
            }
        }

        if (reader.TryGetDecimal(out decimal val2))
        {
            return val2;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
internal class DecimalNullableConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return null;
            }

            if (decimal.TryParse(readStr, out decimal val))
            {
                return val;
            }
        }


        if (reader.TryGetDecimal(out decimal val2))
        {
            return val2;
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteStringValue("");
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
internal class LongNullableConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return null;
            }

            if (long.TryParse(readStr, out long val))
            {
                return val;
            }
        }

        if (reader.TryGetInt64(out long value))
        {
            return value;
        }
        
        return reader.GetInt64();
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString()??"");
    }
}
internal class LongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return 0;
            }

            if (long.TryParse(readStr, out long val))
            {
                return val;
            }
        }

        if (reader.TryGetInt64(out long value))
        {
            return value;
        }

        return reader.GetInt64();
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
internal class IntNullableConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return null;
            }

            if (int.TryParse(readStr, out int val))
            {
                return val;
            }
        }

        if (reader.TryGetInt32(out int value))
        {
            return value;
        }

        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteStringValue("");
        }
        else {
            writer.WriteNumberValue(value.Value);
        }
    }
}
internal class IntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return 0;
            }

            if (int.TryParse(readStr, out int val))
            {
                return val;
            }
        }

        if (reader.TryGetInt32(out int value))
        {
            return value;
        }

        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
internal class DateTimeNullableConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String) 
        {
            var tokenValue = reader.GetString();
            return string.IsNullOrEmpty(tokenValue) ? default(DateTime?) : DateTime.Parse(tokenValue);
        }

        if (reader.TryGetDateTime(out DateTime value)) 
        {
            return value;
        }

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null) 
        {
            writer.WriteStringValue("");
        }
        writer.WriteStringValue(value?.ToString("yyyy-MM-dd HH:mm:ss")??"");
    }
}
internal class DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var tokenValue = reader.GetString()!;
            return DateTime.Parse(tokenValue);
        }

        if (reader.TryGetDateTime(out DateTime value))
        {
            return value;
        }

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
internal class GuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TryGetGuid(out Guid val))
        {
            return val;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return Guid.Empty;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return Guid.Empty;
            }

            if (Guid.TryParse(readStr, out Guid data))
            {
                return data;
            }
        }

        return reader.GetGuid();
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("N"));
    }
}
internal class GuidNullableConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TryGetGuid(out Guid data))
        {
            return data;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var readStr = reader.GetString();
            if (string.IsNullOrWhiteSpace(readStr))
            {
                return null;
            }

            if (Guid.TryParse(readStr, out Guid val))
            {
                return val;
            }
        }

        return reader.GetGuid();
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteStringValue("");
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString("N"));
        }
    }
}
internal class DictionaryLongStringJsonConverter : JsonConverter<Dictionary<long, string>>
{
    public override Dictionary<long, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        var dictionary = new Dictionary<long, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            reader.Read();
            var key = long.Parse(propertyName);
            var value = reader.GetString()!;
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<long, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var key in value.Keys)
        {
            writer.WritePropertyName(key.ToString());
            writer.WriteStringValue(value?[key]?.ToString() ?? "");
        }

        writer.WriteEndObject();
    }
}
internal class StringConverter : JsonConverter<string>
{
    public override string Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.Null => string.Empty,
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            JsonTokenType.Number when reader.TryGetInt16(out short s) => s.ToString(),
            JsonTokenType.Number when reader.TryGetInt32(out int i) => i.ToString(),
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l.ToString(),
            JsonTokenType.Number when reader.TryGetDecimal(out decimal d) => d.ToString(),
            JsonTokenType.Number when reader.TryGetUInt16(out ushort us) => us.ToString(),
            JsonTokenType.Number when reader.TryGetUInt32(out uint ui) => ui.ToString(),
            JsonTokenType.Number when reader.TryGetUInt64(out ulong ul) => ul.ToString(),
            JsonTokenType.Number when reader.TryGetSingle(out float f) => f.ToString(),
            JsonTokenType.Number => reader.GetDouble().ToString(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            JsonTokenType.String => reader.GetString()!,
            _ => throw new System.Text.Json.JsonException("String Converter err")
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

internal class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
            JsonTokenType.String => reader.GetString()!,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    public override void Write(
        Utf8JsonWriter writer,
        object objectToWrite,
        JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
}
