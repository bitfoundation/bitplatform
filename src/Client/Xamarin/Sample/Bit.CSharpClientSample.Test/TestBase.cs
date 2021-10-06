using System;
using System.Reflection;
using System.Threading.Tasks;
using FakeItEasy;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Services;

namespace Bit.CSharpClientSample.Test
{
    [Serializable]
    public abstract class TestBase
    {
        public async Task Run()
        {
            AppDomain domain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"),
                AppDomain.CurrentDomain.Evidence,
                AppDomain.CurrentDomain.SetupInformation);

            try
            {
                domain.DoCallBack(() =>
                {
                    try
                    {
                        Xamarin.Forms.Mocks.MockForms.Init();
                        PopupNavigation.SetInstance(A.Fake<IPopupNavigation>());

                        TestImpl().GetAwaiter().GetResult();
                    }
                    catch (Exception exp) when (exp.GetType().GetCustomAttribute<SerializableAttribute>() == null)
                    {
                        throw new InvalidOperationException(exp.ToString());
                    }
                });
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        protected abstract Task TestImpl();
    }
}
