using System;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Foundation.Core.Contracts;
using Foundation.Test.Core.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Foundation.Api.Implementations;

namespace Foundation.Test.Api
{
    [TestClass]
    public class AppStartupTests
    {
        [TestMethod]
        [TestCategory("Hosting")]
        [ExpectedException(typeof(ObjectDisposedException))]
        public virtual void ContainerMustBeDisposedFinally()
        {
            using (new TestEnvironment())
            {

            }

            DefaultDependencyManager.Current.Resolve<IDependencyManager>();
        }

        [TestMethod]
        [TestCategory("Hosting")]
        public virtual void AllOnAppEndEventsMustBeCalledAtEnd()
        {
            using (new TestEnvironment())
            {

            }

            foreach (IAppEvents appEvents in TestDependencyManager.CurrentTestDependencyManager.Objects
                .OfType<IAppEvents>().ToList())
            {
                A.CallTo(() => appEvents.OnAppEnd())
                    .MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [TestMethod]
        [TestCategory("Hosting")]
        public virtual void AllOnAppStartsMustBeCalledOnceAtStartup()
        {
            using (new TestEnvironment())
            {
                foreach (IAppEvents appEventse in
                    TestDependencyManager.CurrentTestDependencyManager.Objects.OfType<IAppEvents>().ToList())
                {
                    A.CallTo(() => appEventse.OnAppStartup())
                        .MustHaveHappened(Repeated.Exactly.Once);
                }
            }
        }
    }
}
