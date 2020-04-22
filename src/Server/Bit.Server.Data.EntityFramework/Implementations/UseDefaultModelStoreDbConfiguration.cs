using Bit.Core.Implementations;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Bit.Data.EntityFramework.Implementations
{
    public class UseDefaultModelStoreDbConfiguration : DbConfiguration
    {
        public UseDefaultModelStoreDbConfiguration()
        {
            SetModelStore(new DefaultDbModelStore(DefaultPathProvider.Current.GetCurrentAppPath()));
        }
    }
}
