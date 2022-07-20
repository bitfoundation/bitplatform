using System;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Services.Contracts;

public interface IExceptionHandler
{
    void Handle(Exception exception, IDictionary<string, object?>? parameters = null);
}
