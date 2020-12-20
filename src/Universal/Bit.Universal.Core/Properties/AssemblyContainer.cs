using System.Reflection;

#if Android || iOS
[assembly: Bit.Client.Xamarin.Preserve]

namespace Bit.Client.Xamarin
{
    public sealed class PreserveAttribute : System.Attribute
    {
        public bool AllMembers;
        public bool Conditional;
    }

}
#endif

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetUniversalCoreAssembly(this AssemblyContainer container)
        {
            return typeof(AssemblyContainer).GetTypeInfo().Assembly;
        }
    }
}
