namespace Bit.BevTools;

internal static class BevToolsTypeNames
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, string> Cache = new();

    public static string Short(Type type) => Cache.GetOrAdd(type, static t => Format(t));

    private static string Format(Type t)
    {
        if (t.IsGenericType)
        {
            var name = t.Name;
            var tick = name.IndexOf('`');
            if (tick > 0)
            {
                name = name[..tick];
            }

            return name + "<" + string.Join(", ", t.GetGenericArguments().Select(Format)) + ">";
        }

        if (t.IsArray)
        {
            return Format(t.GetElementType()!) + "[]";
        }

        return t.Name;
    }
}
