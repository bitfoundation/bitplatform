namespace Bit.Model.Contracts
{
    public interface IVersionableEntity
    {
        long Version { get; set; }
    }
}
