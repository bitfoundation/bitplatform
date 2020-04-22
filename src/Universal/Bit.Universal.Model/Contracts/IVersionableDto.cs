namespace Bit.Model.Contracts
{
    public interface IVersionableDto : IDto
    {
        long Version { get; set; }
    }
}
