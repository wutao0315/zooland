using System.Dynamic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Extensions.System.Text.Json.Tests")]


namespace System.Text.Json
{
    // Another missing feature out of the box for System.Text.Json
    // https://github.com/dotnet/runtime/issues/31175
    public class DynamicJsonElement : DynamicObject
    {
        protected JsonSerializerOptions Options { get; }
        protected JsonElement Element { get; }

        protected DynamicJsonElement(JsonElement element, JsonSerializerOptions options)
        {
            Element = element;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public static dynamic From(JsonElement element, JsonSerializerOptions? options = null)
        {
            options ??= new JsonSerializerOptions();

            return element.ValueKind switch
            {
                JsonValueKind.Array => new DynamicJsonArray(element, options),
                JsonValueKind.Object => new DynamicJsonObject(element, options),
                _ => new DynamicJsonElement(element, options)
            };
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            _ = binder ?? throw new ArgumentNullException(nameof(binder));

            if (binder.IsValueType<JsonElement>())
            {
                result = Element;
                return true;
            }

            switch (Element.ValueKind)
            {
                case JsonValueKind.Null when binder.Type.IsClass || binder.Type.IsGenericType && binder.Type.GetGenericTypeDefinition() == typeof(Nullable<>):
                    result = null!;
                    return true;

                case JsonValueKind.False when binder.IsValueType<bool>():
                case JsonValueKind.True when binder.IsValueType<bool>():
                    result = Element.GetBoolean();
                    return true;

                case JsonValueKind.String when binder.Type.IsEnum:
                case JsonValueKind.Number when binder.Type.IsEnum:
                    return TryConvertEnum(binder, out result);

                case JsonValueKind.String:
                    return TryConvertString(binder, out result);

                case JsonValueKind.Number:
                    return TryConvertNumber(binder, out result);
            }

            result = null!;
            return false;
        }

        private bool TryConvertNumber(ConvertBinder binder, out object result)
        {
            if (binder.IsValueType<byte>())
            {
                result = Element.GetByte();
            }
            else if (binder.IsValueType<short>())
            {
                result = Element.GetInt16();
            }
            else if (binder.IsValueType<int>())
            {
                result = Element.GetInt32();
            }
            else if (binder.IsValueType<long>())
            {
                result = Element.GetInt64();
            }
            else if (binder.IsValueType<float>())
            {
                result = Element.GetSingle();
            }
            else if (binder.IsValueType<double>())
            {
                result = Element.GetDouble();
            }
            else if (binder.IsValueType<decimal>())
            {
                result = Element.GetDecimal();
            }
            else if (binder.IsValueType<sbyte>())
            {
                result = Element.GetSByte();
            }
            else if (binder.IsValueType<ushort>())
            {
                result = Element.GetUInt16();
            }
            else if (binder.IsValueType<uint>())
            {
                result = Element.GetUInt32();
            }
            else if (binder.IsValueType<ulong>())
            {
                result = Element.GetUInt64();
            }
            else
            {
                result = null!;
            }

            return !(result is null);
        }

        private bool TryConvertEnum(ConvertBinder binder, out object result)
        {
            switch (Element.ValueKind)
            {
                case JsonValueKind.String:
                    result = Enum.Parse(binder.Type, Element.GetString(), true);
                    return true;

                case JsonValueKind.Number:
                    result = Enum.ToObject(binder.Type, Element.GetUInt64());
                    return true;
            }

            result = null!;
            return false;
        }

        private bool TryConvertString(ConvertBinder binder, out object result)
        {
            if (binder.IsType<string>())
            {
                result = Element.GetString();
                return true;
            }

            if (binder.IsValueType<DateTime>() && Element.TryGetDateTime(out var dateTime))
            {
                result = dateTime;
                return true;
            }

            if (binder.IsValueType<DateTimeOffset>() && Element.TryGetDateTimeOffset(out var dateTimeOffset))
            {
                result = dateTimeOffset;
                return true;
            }

            if (binder.IsValueType<TimeSpan>() && TimeSpan.TryParse(Element.GetString(), out var timeSpan))
            {
                result = timeSpan;
                return true;
            }

            if (binder.IsValueType<Guid>() && Guid.TryParse(Element.GetString(), out var guid))
            {
                result = guid;
                return true;
            }

            if (binder.IsType<Uri>() && Uri.TryCreate(Element.GetString(), UriKind.RelativeOrAbsolute, out var uri))
            {
                result = uri;
                return true;
            }

            result = null!;
            return false;
        }

        protected object GetValue(JsonElement element)
            => From(element, Options);

        public override string ToString()
            => Element.GetRawText();
    }
}