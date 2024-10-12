using System.Dynamic;

namespace System.Text.Json
{
    internal static class ConvertBinderExtensions
    {
        public static bool IsType<T>(this ConvertBinder binder)
            => binder.Type == typeof(T);

        public static bool IsValueType<T>(this ConvertBinder binder)
            where T : struct
            => binder.IsType<T>() || binder.Type == typeof(T?);
    }
}
