#if !NET9_0_OR_GREATER
// The attribute .NET 9 brought, for the .NET 8 build: the compiler reads it by its name, so a call that fits two
// overloads resolves the same way whichever build of the library it was compiled against.
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
    {
        public int Priority { get; } = priority;
    }
}
#endif
