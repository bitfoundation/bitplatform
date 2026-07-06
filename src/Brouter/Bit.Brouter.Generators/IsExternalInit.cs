// Polyfill: records/init-only setters require this marker type, which netstandard2.0 lacks.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
