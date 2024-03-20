namespace Zooyard;

public static class ObjectExtensions
{
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
