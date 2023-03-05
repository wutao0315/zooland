using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zooyard.HttpImpl;

public static class ObjectExtensions
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
            new DictionaryLongStringJsonConverter()
        }
    };
    /// <summary>
    /// Convert an object to a JSON string with camelCase formatting
    /// </summary>
    /// <param name="obj"></param>
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

public class BoolConverter : JsonConverter<bool>
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
public class BoolNullableConverter : JsonConverter<bool?>
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
public class LongNullableConverter : JsonConverter<long?>
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
public class LongConverter : JsonConverter<long>
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
public class IntNullableConverter : JsonConverter<int?>
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
public class IntConverter : JsonConverter<int>
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
public class DateTimeNullableConverter : JsonConverter<DateTime?>
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
public class DateTimeConverter : JsonConverter<DateTime>
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
class GuidConverter : JsonConverter<Guid>
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
class GuidNullableConverter : JsonConverter<Guid?>
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
public class DictionaryLongStringJsonConverter : JsonConverter<Dictionary<long, string>>
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
