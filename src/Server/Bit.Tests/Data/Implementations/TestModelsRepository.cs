using Bit.Tests.Data.Contracts;
using Bit.Tests.Model.DomainModels;

namespace Bit.Tests.Data.Implementations
{
    public class TestModelsRepository : TestEfRepository<TestModel>, ITestModelsRepository
    {
    }
}
