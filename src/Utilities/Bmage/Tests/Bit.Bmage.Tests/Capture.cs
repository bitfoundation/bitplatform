namespace Bit.Bmage.Tests;

/// <summary>
/// Runs a piece of code and hands back the exception it threw (or <see langword="null"/> when it completed), so a test
/// can assert on what a call does when it is allowed either to succeed or to fail in a controlled way.
/// </summary>
public static class Capture
{
    public static Exception? Exception(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static Exception? Exception(Func<object?> func)
    {
        try
        {
            func();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static async Task<Exception?> ExceptionAsync(Func<Task> func)
    {
        try
        {
            await func();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
