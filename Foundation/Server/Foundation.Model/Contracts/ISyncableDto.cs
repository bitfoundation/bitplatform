namespace Foundation.Model.Contracts
{
    public interface ISyncableDto : IsArchivableDto , IVersionableDto
    {
        bool ISV { get; set; }
    }
}
