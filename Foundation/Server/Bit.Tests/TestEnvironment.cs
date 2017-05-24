using Bit.Core;
using Foundation.Test.Api.Implementations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Foundation.Test
{
    public class TestEnvironment : TestEnvironmentBase
    {
        static TestEnvironment()
        {
            AppEnvironmentProviderBuilder = args => new TestAppEnvironmentProvider(args);
            DependenciesManagerProviderBuilder = args => new TestDependenciesManagerProvider(args);
        }

        public TestEnvironment(TestEnvironmentArgs args = null)
            : base(args)
        {
        }

        protected override List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            List<Func<TypeInfo, bool>> baseList = base.GetAutoProxyCreationIncludeRules();

            baseList.AddRange(new List<Func<TypeInfo, bool>>
            {
                serviceType => serviceType.Assembly == AssemblyContainer.Current.GetBitTestsAssembly()
            });

            return baseList;
        }
    }
}