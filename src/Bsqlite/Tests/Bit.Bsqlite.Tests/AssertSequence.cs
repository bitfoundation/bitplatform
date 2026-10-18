namespace Bit.Bsqlite;

/// <summary>
/// Element-by-element equality of two sequences. MSTest's <see cref="Assert.AreEqual{T}(T, T)"/> compares
/// a sequence as one object - two arrays with the same contents are not equal to it - so every assertion
/// about what a list holds goes through here.
/// </summary>
internal static class AssertSequence
{
    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T>? actual)
    {
        Assert.IsNotNull(actual);
        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
    }
}
