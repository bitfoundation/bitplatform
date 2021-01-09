using Bit.Core.Contracts;
using Bit.Owin.Implementations;
using Bit.Test.Implementations;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Bit.Tests.Api
{
    [TestClass]
    public class AppStartupTests
    {
        [TestMethod]
        [TestCategory("Hosting")]
        public virtual void AllOnAppStartsMustBeCalledOnceAtStartup()
        {
            using (BitOwinTestEnvironment testEnvironment = new BitOwinTestEnvironment())
            {
                foreach (IAppEvents appEvents in testEnvironment.GetObjects<IAppEvents>())
                {
                    A.CallTo(() => appEvents.OnAppStartup())
                        .MustHaveHappenedOnceExactly();
                }
            }
        }
    }
}
