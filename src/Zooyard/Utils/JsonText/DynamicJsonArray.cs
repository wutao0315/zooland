using System.Dynamic;

namespace System.Text.Json
{
    internal sealed class DynamicJsonArray : DynamicJsonElement
    {
        internal DynamicJsonArray(JsonElement element, JsonSerializerOptions options)
            : base(element, options)
        {
            if (element.ValueKind != JsonValueKind.Array)
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

            if (binder.Type.IsArray)
            {
                result = ToArray(binder.Type.GetElementType());
                return true;
            }

            if (binder.Type.IsGenericType && binder.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                result = ToArray(binder.Type.GetGenericArguments()[0]);
                return true;
            }

            result = null!;
            return false;
        }

        private Array ToArray(Type type)
        {
            var length = Element.GetArrayLength();
            var array = (Array)Activator.CreateInstance(type.MakeArrayType(), length);
            var enumerator = Element.EnumerateArray();
            var index = 0;
            var isJsonElement = type == typeof(JsonElement);
            var isDynamic = type == typeof(object);
            var isJsonElementOrDynamic = isJsonElement || isDynamic;

            while (enumerator.MoveNext())
            {
                object value = enumerator.Current.ValueKind switch
                {
                    JsonValueKind.False when !isJsonElementOrDynamic => false,
                    JsonValueKind.True when !isJsonElementOrDynamic => true,
                    JsonValueKind.Number when !isJsonElementOrDynamic => enumerator.Current.GetDecimal(),
                    JsonValueKind.String when !isJsonElementOrDynamic => enumerator.Current.GetString(),
                    _ when isJsonElement => enumerator.Current,
                    _ when isDynamic => DynamicJsonElement.From(enumerator.Current, Options),
                    _ => enumerator.Current.GetRawText()
                };

                if (!isJsonElementOrDynamic)
                {
                    value = Convert.ChangeType(value, type);
                }

                array.SetValue(value, index++);
            }

            return array;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            _ = indexes ?? throw new ArgumentNullException(nameof(indexes));

            if (indexes.Length == 1 && indexes[0] is int index && Element.GetArrayLength() >= index)
            {
                result = GetValue(Element.EnumerateArray().ElementAt(index));
                return true;
            }

            result = null!;
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            _ = binder ?? throw new ArgumentNullException(nameof(binder));

            var comparer = binder.IgnoreCase
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;

            if (string.Equals(binder.Name, "Length", comparer) || string.Equals(binder.Name, "Count", comparer))
            {
                result = Element.GetArrayLength();
                return true;
            }

            result = null!;
            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            _ = binder ?? throw new ArgumentNullException(nameof(binder));

            var comparer = binder.IgnoreCase
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;

            if (string.Equals(binder.Name, "Count", comparer) && binder.CallInfo.ArgumentCount == 0 &&
                (binder.ReturnType == typeof(int) || binder.ReturnType == typeof(object)))
            {
                result = Element.GetArrayLength();
                return true;
            }

            result = null!;
            return false;
        }
    }
}