using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public interface IPopupNavigationService
    {
        Task PushAsync(string name);

        Task PopAllAsync();

        Task PopAsync();
    }
}
