using Bit.Core;
using Foundation.Test.Api.Implementations;
using System;
using System.Collections.Generic;
using System.Reflection;
using Foundation.Core.Contracts;

namespace Foundation.Test
{
    public class TestEnvironment : TestEnvironmentBase
    {
        public TestEnvironment(TestEnvironmentArgs args = null)
            : base(args)
        {
        }

        protected override IDependenciesManagerProvider GetDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            return args.CustomDependenciesManagerProvider ?? new TestDependenciesManagerProvider(args);
        }

        protected override IAppEnvironmentProvider GetAppEnvironmentProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppEnvironmentProvider ?? new TestAppEnvironmentProvider(args);
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