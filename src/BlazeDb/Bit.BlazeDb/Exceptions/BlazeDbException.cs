namespace Bit.BlazeDb;

/// <summary>Base type for all BlazeDb-specific errors.</summary>
public class BlazeDbException : Exception
{
    public BlazeDbException(string message) : base(message)
    {
    }

    public BlazeDbException(string message, Exception inner) : base(message, inner)
    {
    }
}
