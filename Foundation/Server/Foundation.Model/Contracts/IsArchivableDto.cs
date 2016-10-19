namespace Foundation.Model.Contracts
{
    public interface IsArchivableDto : IDto
    {
        bool IsArchived { get; set; }
    }
}
