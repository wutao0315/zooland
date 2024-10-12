using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zooyard.Utils.JsonConverters;

/// <summary>
/// System.Text.Json原生不支持DataTable的序列化，需自定义实现
/// 本类代码来自于：https://github.com/dotnet/docs/blob/main/docs/standard/serialization/system-text-json/snippets/how-to/csharp/RoundtripDataTable.cs
/// </summary>
public class DataTableJsonConverter : JsonConverter<DataTable>
{
    public override DataTable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        JsonElement rootElement = jsonDoc.RootElement;
        DataTable dataTable = rootElement.JsonElementToDataTable();
        return dataTable;
    }

    public override void Write(Utf8JsonWriter jsonWriter, DataTable value, JsonSerializerOptions options)
    {
        jsonWriter.WriteStartArray();
        foreach (DataRow dr in value.Rows)
        {
            jsonWriter.WriteStartObject();
            foreach (DataColumn col in value.Columns)
            {
                string key = col.ColumnName.Trim();

                Action<string> action = GetWriteAction(dr, col, jsonWriter);
                action.Invoke(key);

                static Action<string> GetWriteAction(
                    DataRow row, DataColumn column, Utf8JsonWriter writer) => row[column] switch
                    {
                        // bool
                        bool value => key => writer.WriteBoolean(key, value),

                        // numbers
                        byte value => key => writer.WriteNumber(key, value),
                        sbyte value => key => writer.WriteNumber(key, value),
                        decimal value => key => writer.WriteNumber(key, value),
                        double value => key => writer.WriteNumber(key, value),
                        float value => key => writer.WriteNumber(key, value),
                        short value => key => writer.WriteNumber(key, value),
                        int value => key => writer.WriteNumber(key, value),
                        ushort value => key => writer.WriteNumber(key, value),
                        uint value => key => writer.WriteNumber(key, value),
                        ulong value => key => writer.WriteNumber(key, value),

                        // strings
                        DateTime value => key => writer.WriteString(key, value),
                        Guid value => key => writer.WriteString(key, value),

                        _ => key => writer.WriteString(key, row[column].ToString())
                    };
            }
            jsonWriter.WriteEndObject();
        }
        jsonWriter.WriteEndArray();
    }
}

internal static class JsonExtensions
{
    public static DataTable JsonElementToDataTable(this JsonElement dataRoot)
    {
        var dataTable = new DataTable();
        bool firstPass = true;
        foreach (JsonElement element in dataRoot.EnumerateArray())
        {
            DataRow row = dataTable.NewRow();
            dataTable.Rows.Add(row);
            foreach (JsonProperty col in element.EnumerateObject())
            {
                if (firstPass)
                {
                    JsonElement colValue = col.Value;
                    dataTable.Columns.Add(new DataColumn(col.Name, colValue.ValueKind.ValueKindToType(colValue.ToString()!)));
                }
                row[col.Name] = col.Value.JsonElementToTypedValue();
            }
            firstPass = false;
        }

        return dataTable;
    }

    private static Type ValueKindToType(this JsonValueKind valueKind, string value)
    {
        switch (valueKind)
        {
            case JsonValueKind.String:
                return typeof(string);
            case JsonValueKind.Number:
                if (long.TryParse(value, out _))
                {
                    return typeof(long);
                }
                else
                {
                    return typeof(double);
                }
            case JsonValueKind.True:
            case JsonValueKind.False:
                return typeof(bool);
            case JsonValueKind.Undefined:
                throw new NotSupportedException();
            case JsonValueKind.Object:
                return typeof(object);
            case JsonValueKind.Array:
                return typeof(Array);
            case JsonValueKind.Null:
                throw new NotSupportedException();
            default:
                return typeof(object);
        }
    }

    private static object? JsonElementToTypedValue(this JsonElement jsonElement)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                throw new NotSupportedException();
            case JsonValueKind.String:
                if (jsonElement.TryGetGuid(out Guid guidValue))
                {
                    return guidValue;
                }
                else
                {
                    if (jsonElement.TryGetDateTime(out DateTime datetime))
                    {
                        // If an offset was provided, use DateTimeOffset.
                        if (datetime.Kind == DateTimeKind.Local)
                        {
                            if (jsonElement.TryGetDateTimeOffset(out DateTimeOffset datetimeOffset))
                            {
                                return datetimeOffset;
                            }
                        }
                        return datetime;
                    }
                    return jsonElement.ToString();
                }
            case JsonValueKind.Number:
                if (jsonElement.TryGetInt64(out long longValue))
                {
                    return longValue;
                }
                else
                {
                    return jsonElement.GetDouble();
                }
            case JsonValueKind.True:
            case JsonValueKind.False:
                return jsonElement.GetBoolean();
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
                return null;
            default:
                return jsonElement.ToString();
        }
    }
}
