using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;

namespace Bit.Data.EntityFramework.Implementations
{
    public class UseDefaultModelStoreDbConfiguration : DbConfiguration
    {
        public UseDefaultModelStoreDbConfiguration()
        {
            SetModelStore(new DefaultDbModelStore(Directory.GetCurrentDirectory()));
        }
    }
}
