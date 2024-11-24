﻿namespace Boilerplate.Client.Core.Services;

public partial class ModalData(Type type, IDictionary<string, object>? parameters, string? title, TaskCompletionSource<bool> taskCompletionSource)
{
    public Type ComponentType { get; set; } = type;

    public IDictionary<string, object>? Parameters { get; set; } = parameters;

    public string? Title { get; set; } = title;

    public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = taskCompletionSource;
}
