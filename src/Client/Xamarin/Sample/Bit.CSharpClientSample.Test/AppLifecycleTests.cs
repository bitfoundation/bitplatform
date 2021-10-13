using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.CSharpClientSample.ViewModels;
using Bit.CSharpClientSample.Views;
using Bit.Http.Contracts;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xamarin.Forms;

namespace Bit.CSharpClientSample.Test
{
    [TestClass, Serializable]
    public class NotLoggedInUserLandingPageTest : TestBase
    {
        [TestMethod]
        public Task NotLoggedInUserLandingPageTestRun() => Run();

        protected async override Task TestImpl()
        {
            await using var app = new App(new TestPlatformInitializer((dependencyManager, containerRegistry, containerBuilder, services) =>
            {
                dependencyManager.RegisterInstance(A.Fake<ISecurityService>(), servicesType: new[] { typeof(ISecurityServiceBase).GetTypeInfo(), typeof(ISecurityService).GetTypeInfo() });
            }));

            await app.Ready();

            Assert.IsTrue(app.MainPage is NavigationPage navPage && navPage.CurrentPage is LoginView);
        }
    }

    [TestClass, Serializable]
    public class LoggedInUserLandingPageTest : TestBase
    {
        [TestMethod]
        public Task LoggedInUserLandingPageTestRun() => Run();

        protected async override Task TestImpl()
        {
            await using var app = new App(new TestPlatformInitializer((dependencyManager, containerRegistry, containerBuilder, services) =>
            {
                var securityService = A.Fake<ISecurityService>();

                A.CallTo(() => securityService.IsLoggedInAsync(A<CancellationToken>.Ignored)).Returns(Task.FromResult(true));

                dependencyManager.RegisterInstance(securityService, servicesType: new[] { typeof(ISecurityServiceBase).GetTypeInfo(), typeof(ISecurityService).GetTypeInfo() });
            }));

            await app.Ready();

            Assert.IsTrue(app.MainPage is NavigationPage navPage && navPage.CurrentPage is MainView);

            await ((MainViewModel)((NavigationPage)app.MainPage).CurrentPage.BindingContext).LogoutCommand.ExecuteAsync();

            Assert.IsTrue(app.MainPage is NavigationPage navPage2 && navPage2.CurrentPage is LoginView);
        }
    }
}
