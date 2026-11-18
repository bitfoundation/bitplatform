namespace Bit.Bink;

/// <summary>
/// A path an element built, together with the inputs it was built from. Elements are records, so a <c>with</c>
/// expression copies the cache into an instance whose inputs may differ; checking the key keeps that copy from
/// drawing the original's path. Key and path live in one immutable object so a reader can never see a new key
/// paired with an old path.
/// </summary>
internal sealed class BinkCachedPath<TKey>(TKey key, BinkPathGeometry path)
{
    public BinkPathGeometry Path { get; } = path;

    public bool Matches(in TKey candidate) => EqualityComparer<TKey>.Default.Equals(key, candidate);
}
