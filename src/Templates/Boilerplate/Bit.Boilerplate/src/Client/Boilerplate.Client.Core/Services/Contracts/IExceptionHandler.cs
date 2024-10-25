using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IExceptionHandler
{
    void Handle(Exception exception, 
        IDictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "");
}
