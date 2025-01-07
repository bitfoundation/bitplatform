using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services.Contracts;

public enum ExceptionDisplayKind
{
    /// <summary>
    /// No error message is shown to the user.
    /// </summary>
    None,
    /// <summary>
    /// Requires the user to acknowledge the error (e.g., by tapping "OK").
    /// </summary>
    Interrupting,
    /// <summary>
    /// Shows an auto-dismissed message (e.g., a toast notification)
    /// </summary>
    NonInterrupting,
    /// <summary>
    /// Automatically selects the exception display type based on the exception category.
    /// </summary>
    Default
}

public interface IExceptionHandler
{
    void Handle(Exception exception,
        ExceptionDisplayKind displayKind = ExceptionDisplayKind.Default,
        Dictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "");
}
