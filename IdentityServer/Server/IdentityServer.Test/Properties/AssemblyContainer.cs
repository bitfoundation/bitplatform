using Foundation.Test;
using IdentityServer.Test.Api;
using System.Reflection;

namespace Bit.Core
{
    public static class AssemblyContainerExtensions
    {
        public static Assembly GetBitIdentityServerTestsAssembly(this AssemblyContainer container)
        {
            return typeof(CodeBasedLoginTests).GetTypeInfo().Assembly;
        }
    }
}
