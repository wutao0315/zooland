using System.Dynamic;

namespace System.Text.Json
{
    internal sealed class DynamicJsonObject : DynamicJsonElement
    {
        internal DynamicJsonObject(JsonElement element, JsonSerializerOptions options)
            : base(element, options)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentOutOfRangeException(nameof(element));
            }
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            _ = binder ?? throw new ArgumentNullException(nameof(binder));

            if (binder.Type == typeof(JsonElement))
            {
                result = Element;
                return true;
            }

            if ((binder.Type.IsClass && !binder.Type.IsAbstract && !(binder.Type.GetConstructor(Type.EmptyTypes) is null))
                || (binder.Type.IsGenericType && binder.Type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    Element.WriteTo(writer);
                }

                result = JsonSerializer.Deserialize(stream.ToArray(), binder.Type, Options);
                return true;
            }

            result = null!;
            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (var property in Element.EnumerateObject())
            {
                yield return property.Name;
            }
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            _ = indexes ?? throw new ArgumentNullException(nameof(indexes));

            if (indexes.Length == 1 && indexes[0] is string propertyName && Element.TryGetProperty(propertyName, out var element))
            {
                result = GetValue(element);
                return true;
            }

            result = null!;
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            _ = binder ?? throw new ArgumentNullException(nameof(binder));

            if (Element.TryGetProperty(binder.Name, out var property))
            {
                result = GetValue(property);
                return true;
            }

            result = null!;
            return false;
        }
    }
}