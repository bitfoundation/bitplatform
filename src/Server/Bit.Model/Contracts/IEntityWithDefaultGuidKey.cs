using System;

namespace Bit.Model.Contracts
{
    [Obsolete]
    public interface IEntityWithDefaultGuidKey : IEntity
    {
        Guid Id { get; set; }
    }
}
