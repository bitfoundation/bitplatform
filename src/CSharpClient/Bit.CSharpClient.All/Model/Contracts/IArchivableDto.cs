namespace Bit.Model.Contracts
{
    public interface IArchivableDto : IDto
    {
        bool IsArchived { get; set; }
    }
}
