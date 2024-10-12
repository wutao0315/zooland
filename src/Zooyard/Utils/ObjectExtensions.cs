using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Zooyard.Utils.JsonConverters;

namespace Zooyard.Utils;


internal static class ObjectExtensions
{
    public readonly static JsonSerializerOptions _option = GetDefaultOption();
    internal static JsonSerializerOptions GetDefaultOption(JsonNamingPolicy? policy = null, string formatString = "yyyy-MM-dd HH:mm:ss.fff")
    {
        var result = new JsonSerializerOptions
        {
            // 解决中文序列化被编码的问题
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = policy ?? JsonNamingPolicy.CamelCase,
            //DictionaryKeyPolicy = policy ?? JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            // 解决属性名称大小写敏感问题 
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            AllowTrailingCommas = true,
            Converters = {
                new IntConverter(),
                new IntNullableConverter(),
                new LongConverter(),
                new LongNullableConverter(),
                new DateTimeConverter(formatString),
                new DateTimeNullableConverter(formatString),
                new BoolConverter(),
                new BoolNullableConverter(),
                new DecimalConverter(),
                new DecimalNullableConverter(),
                new GuidConverter(),
                new GuidNullableConverter(),
                new DictionaryLongStringJsonConverter(),
                new JsonStringEnumConverter(),
                new StringConverter(),
                new DataTableJsonConverter()
            }
        };
        return result;
    }
    /// <summary>
    /// Convert an object to a JSON string with camelCase formatting
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="empty"></param>
    /// <param name="options"> options </param>
    /// <returns></returns>
    public static string ToJsonString(this object obj, string empty = "", JsonSerializerOptions? options = default)
    {
        if (obj == null)
        {
            return empty;
        }

        if (options != null)
            return JsonSerializer.Serialize(obj, options);
        else
            return JsonSerializer.Serialize(obj, _option);
    }
    /// <summary>
    /// Object to json string byte array.
    /// </summary>
    /// <param name="obj"> obj </param>
    /// <param name="options"> options </param>
    /// <returns> json string byte array </returns>
    public static byte[] ToJsonBytesThrow(this object obj, JsonSerializerOptions? options = default)
    {
        try
        {
            if (options != null)
                return JsonSerializer.SerializeToUtf8Bytes(obj, options);
            else
                return JsonSerializer.SerializeToUtf8Bytes(obj, _option);
        }
        catch (Exception e)
        {
            throw;
        }
    }
    /// <summary>
    /// Deserializes the json.
    /// </summary>
    /// <param name="str">The STR.</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="options">default options</param>
    /// <returns></returns>
    public static T DeserializeJson<T>(this string str, T defaultValue = default!, JsonSerializerOptions? options = default)
    {
        try
        {
            var result = str.DeserializeJsonThrow(defaultValue, options);
            return result;
        }
        catch (Exception ex)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Deserializes the json.
    /// </summary>
    /// <param name="str">The STR.</param>
    /// <param name="defaultValue">default value</param>
    /// <param name="options">default options</param>
    /// <returns></returns>
    public static T DeserializeJsonThrow<T>(this string str, T defaultValue = default!, JsonSerializerOptions? options = default)
    {
        if (string.IsNullOrEmpty(str))
        {
            return defaultValue;
        }

        try
        {
            if (options != null)
            {
                if (typeof(T).FullName == typeof(object).FullName)
                    options.Converters.Add(new DynamicJsonConverter());

                return JsonSerializer.Deserialize<T>(str, options)!;
            }
            else
            {
                if (typeof(T).FullName == typeof(object).FullName)
                    _option.Converters.Add(new DynamicJsonConverter());

                return JsonSerializer.Deserialize<T>(str, _option)!;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Deserializes the json.
    /// </summary>
    /// <param name="str">The STR.</param>
    /// <param name="returnType">return type</param>
    /// <param name="options">default options</param>
    /// <returns></returns>
    public static object? DeserializeJson(this string str, Type returnType, JsonSerializerOptions? options = default)
    {
        try
        {
            if (options != null)
                return JsonSerializer.Deserialize(str, returnType, options)!;
            else
                return JsonSerializer.Deserialize(str, returnType, _option)!;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<T> DeserializeJsonAsync<T>(this Stream utf8stream, JsonSerializerOptions? options = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (utf8stream == null)
        {
            throw new Exception(" utf8stream is null");
        }
        if (options != null)
            return (await JsonSerializer.DeserializeAsync<T>(utf8stream, options, cancellationToken))!;
        else
            return (await JsonSerializer.DeserializeAsync<T>(utf8stream, _option, cancellationToken))!;
    }
    /// <summary>
    /// 根据对象实例和属性名称获得属性值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    public static T GetProperty<T>(this object obj, string property)
    {
        var result = default(T);
        try
        {
            var t = obj.GetType();
            var propertyObj = t.GetProperty(property)?.GetValue(obj, null);
            result = (T)propertyObj.ChangeType(typeof(T))!;
        }
        catch
        {
        }
        return result!;
    }
    public static object? ChangeType(this object? value, Type type)
    {
        if (value == null && type.IsGenericType) return Activator.CreateInstance(type);
        if (value == null) return null;
        if (type == value.GetType()) return value;
        if (type.IsEnum)
        {
            if (value is string val)
                return Enum.Parse(type, val);
            else
                return Enum.ToObject(type, value);
        }
        if (!type.IsInterface && type.IsGenericType)
        {
            Type innerType = type.GetGenericArguments()[0];
            object? innerValue = ChangeType(value, innerType);
            return Activator.CreateInstance(type, [innerValue]);
        }
        if (type == typeof(string))
        {
            return value.ToString();
        }
        if (value is string valGuid && type == typeof(Guid)) return new Guid(valGuid);
        if (value is string valVersion && type == typeof(Version)) return new Version(valVersion);
        if (value is not IConvertible) return value;

        if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            type = underlyingType ?? type;
        } // end if
        return Convert.ChangeType(value, type);
    }
}

#region Conerter 集合类
public class BoolConverter : JsonConverter<bool>
{
    public override bool HandleNull => true;
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
    public override bool HandleNull => true;
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
        else
        {
            writer.WriteBooleanValue(value.Value);
        }
    }
}
public class DecimalConverter : JsonConverter<decimal>
{
    public override bool HandleNull => true;
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
public class DecimalNullableConverter : JsonConverter<decimal?>
{
    public override bool HandleNull => true;
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
public class LongNullableConverter : JsonConverter<long?>
{
    public override bool HandleNull => true;
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
        writer.WriteStringValue(value?.ToString() ?? "");
    }
}
public class LongConverter : JsonConverter<long>
{
    public override bool HandleNull => true;
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
    public override bool HandleNull => true;
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
public class IntConverter : JsonConverter<int>
{
    public override bool HandleNull => true;
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
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
public class DateTimeNullableConverter(string formatString = "yyyy-MM-dd HH:mm:ss.fff") : JsonConverter<DateTime?>
{
    public override bool HandleNull => true;
    public string FormatString => formatString;
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var tokenValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                return null;
            }

            if (DateTime.TryParse(tokenValue, out var dt))
            {
                return dt;
            }

            return DateTime.Parse(tokenValue);

        }

        if (reader.TryGetDateTime(out DateTime value))
        {
            return value;
        }

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null || value == DateTime.MinValue)
        {
            writer.WriteStringValue("");
        }
        else
        {
            writer.WriteStringValue(value?.ToString(FormatString) ?? "");
        }
    }
}
public class DateTimeConverter(string formatString = "yyyy-MM-dd HH:mm:ss.fff") : JsonConverter<DateTime>
{
    public override bool HandleNull => true;
    public string FormatString => formatString;
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
        {
            return DateTime.MinValue;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var tokenValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParse(tokenValue, out var dt))
            {
                return dt;
            }

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
        if (value == DateTime.MinValue)
        {
            writer.WriteStringValue("");
        }
        else
        {
            writer.WriteStringValue(value.ToString(FormatString));
        }
    }
}
public class GuidConverter : JsonConverter<Guid>
{
    public override bool HandleNull => true;
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {

        if (reader.TokenType == JsonTokenType.Null)
        {
            return Guid.Empty;
        }

        if (reader.TryGetGuid(out Guid val))
        {
            return val;
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
        writer.WriteStringValue(value.ToString());
    }
}
public class GuidNullableConverter : JsonConverter<Guid?>
{
    public override bool HandleNull => true;
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
            writer.WriteNullValue();
        }
        else if (value == Guid.Empty)
        {
            writer.WriteStringValue(Guid.Empty.ToString());
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString());
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
public class StringConverter : JsonConverter<string>
{
    public override bool HandleNull => true;
    public override string Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.None => string.Empty,
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
            JsonTokenType.String => reader.GetString() ?? "",
            _ => throw new JsonException("String Converter err")
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? "");
    }
}
public class ObjectToInferredTypesConverter : JsonConverter<object>
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
            JsonTokenType.String => reader.GetString() ?? "",
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    public override void Write(
        Utf8JsonWriter writer,
        object objectToWrite,
        JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
}

#endregion

