using System;
using System.Collections.Generic;
using System.Reflection;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Test;

namespace Bit.Tests
{
    public class BitOwinTestEnvironment : TestEnvironmentBase
    {
        static TestEnvironmentArgs ConfigureArgs(TestEnvironmentArgs args)
        {
            args ??= new TestEnvironmentArgs { };
            return args;
        }

        public BitOwinTestEnvironment(TestEnvironmentArgs args = null)
            : base(ConfigureArgs(args))
        {

        }

        protected override IAppModulesProvider GetAppModulesProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppModulesProvider ?? new BitOwinCoreTestAppModulesProvider(args);
        }

        protected override IAppEnvironmentsProvider GetAppEnvironmentsProvider(TestEnvironmentArgs args)
        {
            return args.CustomAppEnvironmentsProvider ?? new BitTestAppEnvironmentsProvider(args);
        }

        protected override List<Func<TypeInfo, bool>> GetAutoProxyCreationIncludeRules()
        {
            List<Func<TypeInfo, bool>> baseList = base.GetAutoProxyCreationIncludeRules();

            baseList.AddRange(new List<Func<TypeInfo, bool>>
            {
                implementationType => implementationType.Assembly == AssemblyContainer.Current.GetBitTestsAssembly()
            });

            return baseList;
        }
    }
}