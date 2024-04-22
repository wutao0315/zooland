namespace Zooyard.Utils;

internal static class CaseSensitiveEqualHelper
{
    internal static bool Equals(IList<string>? list1, IList<string>? list2)
    {
        return CollectionEqualityHelper.Equals(list1, list2, StringComparer.Ordinal);
    }

    internal static bool Equals(IDictionary<string, string>? dictionary1, IDictionary<string, string>? dictionary2)
    {
        return CollectionEqualityHelper.Equals(dictionary1, dictionary2, StringComparer.Ordinal);
    }

    internal static bool Equals(IList<IDictionary<string, string>>? dictionaryList1, IList<IDictionary<string, string>>? dictionaryList2)
    {
        return CollectionEqualityHelper.Equals(dictionaryList1, dictionaryList2, StringComparer.Ordinal);
    }

    internal static int GetHashCode(IList<string>? values)
    {
        return CollectionEqualityHelper.GetHashCode(values, StringComparer.Ordinal);
    }

    internal static int GetHashCode(IDictionary<string, string>? dictionary)
    {
        return CollectionEqualityHelper.GetHashCode(dictionary, StringComparer.Ordinal);
    }

    internal static int GetHashCode(IList<IDictionary<string, string>>? dictionaryList)
    {
        return CollectionEqualityHelper.GetHashCode(dictionaryList);
    }
}
