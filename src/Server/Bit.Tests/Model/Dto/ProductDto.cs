using Bit.Model.Contracts;

namespace Bit.Tests.Model.Dto
{
    public class ProductDto : IDto
    {
        public int Id { get; set; }

        public int BuildLocationId { get; set; }

        public int BuildLocationId_InMemoryTest { get; set; }

        public int BuildLocationId_OfflineDatabaseTest { get; set; }

        public int BuildLocationId_OnlineTest { get; set; }
    }

    public class CountryDto : IDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int SomeProperty { get; set; }
    }
}
