namespace Bit.BlazeDb;

/// <summary>
/// Thrown by a write - or a commit - that did complete, when one or more <c>Changed</c> handlers threw while
/// the change was being reported. The data is committed: do not retry the write. <see cref="Exception.InnerException"/>
/// is the handler's exception, or an <see cref="AggregateException"/> when several threw. Every subscriber
/// was still notified.
/// </summary>
public sealed class BlazeDbChangeHandlerException : BlazeDbException
{
    public BlazeDbChangeHandlerException(Exception inner)
        : base("The write was committed, but a Changed handler threw while it was being reported. The data is saved; " +
               "do not retry the write. See the inner exception for the handler's error.", inner)
    {
    }
}
