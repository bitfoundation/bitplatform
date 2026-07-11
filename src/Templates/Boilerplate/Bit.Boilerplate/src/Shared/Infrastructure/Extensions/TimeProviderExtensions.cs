namespace System;

public static class TimeProviderExtensions
{
    /// <summary>
    /// By disabling test parallelism, we can use this static property to override the default TimeProvider.System with a custom implementation for testing purposes.
    /// This instance must be used in places where passing a TimeProvider instance is not easy.
    /// </summary>
    public static TimeProvider Instance { get; set; } = TimeProvider.System;

    extension(TimeProvider timeProvider)
    {
        /// <summary>
        /// <inheritdoc cref="TimeProvider.GetUtcNow"/>
        /// </summary>
        /// <returns></returns>
        public static DateTimeOffset GetUtcNow() => Instance.GetUtcNow();
    }
}
