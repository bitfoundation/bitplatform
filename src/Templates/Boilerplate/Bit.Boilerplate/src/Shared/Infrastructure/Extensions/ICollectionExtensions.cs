namespace System.Collections.Generic;

public static partial class ICollectionExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<(T item, int index)> Indexed()
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
