// Polyfill required for record types and init-only setters when targeting netstandard2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
