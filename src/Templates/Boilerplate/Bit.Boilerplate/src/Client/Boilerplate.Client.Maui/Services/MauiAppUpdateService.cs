using Maui.AppStores;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiAppUpdateService : IAppUpdateService
{
    public async Task Update()
    {
        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
    }
}
