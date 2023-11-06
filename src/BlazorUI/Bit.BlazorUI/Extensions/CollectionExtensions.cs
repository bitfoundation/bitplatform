namespace Bit.BlazorUI;

internal static class CollectionExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> readOnlyList, T element)
    {
        if (readOnlyList is IList<T> list)
        {
            return list.IndexOf(element);
        }

        for (int i = 0; i < readOnlyList.Count; i++)
        {
            if (element.Equals(readOnlyList[i]))
            {
                return i;
            }
        }
        return -1;
    }
}
