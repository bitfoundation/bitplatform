using System;
using System.Linq;
using Bit.Core.Contracts;
using Bit.Owin.Implementations;
using Bit.Test.Implementations;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Tests.Api
{
    [TestClass]
    public class AppStartupTests
    {
        [TestMethod]
        [TestCategory("Hosting")]
        [ExpectedException(typeof(ObjectDisposedException))]
        public virtual void ContainerMustBeDisposedFinally()
        {
            using (new BitOwinTestEnvironment())
            {

            }

            DefaultDependencyManager.Current.Resolve<IDependencyManager>();
        }

        [TestMethod]
        [TestCategory("Hosting")]
        public virtual void AllOnAppEndEventsMustBeCalledAtEnd()
        {
            using (new BitOwinTestEnvironment())
            {

            }

            foreach (IAppEvents appEvents in TestDependencyManager.CurrentTestDependencyManager.Objects
                .OfType<IAppEvents>().ToList())
            {
                A.CallTo(() => appEvents.OnAppEnd())
                    .MustHaveHappenedOnceExactly();
            }
        }

        [TestMethod]
        [TestCategory("Hosting")]
        public virtual void AllOnAppStartsMustBeCalledOnceAtStartup()
        {
            using (new BitOwinTestEnvironment())
            {
                foreach (IAppEvents appEvents in
                    TestDependencyManager.CurrentTestDependencyManager.Objects.OfType<IAppEvents>().ToList())
                {
                    A.CallTo(() => appEvents.OnAppStartup())
                        .MustHaveHappenedOnceExactly();
                }
            }
        }
    }
}
