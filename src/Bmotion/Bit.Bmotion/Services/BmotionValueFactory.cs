namespace Bit.Bmotion;

/// <summary>Factory helper for creating BmotionValues.</summary>
public static class BmotionValueFactory
{
    public static BmotionValue<T> Create<T>(T initial) where T : struct
        => new($"mv_{Guid.NewGuid():N}", initial);
}
