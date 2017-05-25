using System;
using System.Collections.Generic;
using System.Reflection;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Test;
using Bit.Tests.Api.Implementations;

namespace Bit.Tests
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