namespace Bit.BlazeDb;

/// <summary>Op codes used in WAL commit payloads.</summary>
internal static class BlazeDbWalOp
{
    public const byte Set = 1;
    public const byte Delete = 2;
}
