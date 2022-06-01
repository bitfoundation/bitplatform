using System.Diagnostics;
using TodoTemplate.App.Shared;

namespace TodoTemplate.App.Services.Implementations;

public class TodoTemplateExceptionHandler : IExceptionHandler
{
    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
#if DEBUG
        Debugger.Break();

        Console.WriteLine(exception.ToString());
#endif

        MessageBox.Show(exception.Message, "Error");
    }
}
