# Stage 4: Background Jobs and CancellationToken Management

Welcome to **Stage 4** of the Boilerplate project tutorial! In this stage, we'll explore how the project handles **cancellation tokens** for request cancellation and **background job processing** with Hangfire.

---

## Table of Contents

1. [CancellationToken in API Requests](#cancellationtoken-in-api-requests)
   - [Automatic Request Cancellation](#automatic-request-cancellation)
   - [Client-Side Integration](#client-side-integration)
   - [User Abandonment Scenarios](#user-abandonment-scenarios)
2. [Navigation Lock for Critical Operations](#navigation-lock-for-critical-operations)
3. [When to Use Background Jobs](#when-to-use-background-jobs)
4. [Background Job Implementation with Hangfire](#background-job-implementation-with-hangfire)
   - [PhoneServiceJobsRunner Example](#phoneservicejobsrunner-example)
   - [Key Benefits of Hangfire Integration](#key-benefits-of-hangfire-integration)

---

## CancellationToken in API Requests

### Automatic Request Cancellation

All API methods in this project receive a `CancellationToken` parameter that **automatically cancels operations** when:

- The **user navigates away** from a page
- The **browser/app is closed**
- A **new request supersedes a previous one** (e.g., navigating to page 2 of a data grid while page 1 is still loading)
- The **component is disposed** in Blazor

**Important Note**: For API methods that return `IQueryable`, cancellation happens **implicitly** - you don't need to manually pass the token to EF Core queries because OData and Entity Framework Core handle it automatically.

This ensures that server resources are not wasted processing requests that the user no longer needs.

#### Server-Side Example

Let's look at a real controller from the project - [`TodoItemController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Todo/TodoItemController.cs):

```csharp
[HttpPost]
public async Task<TodoItemDto> Create(TodoItemDto dto, CancellationToken cancellationToken)
{
    var entityToAdd = dto.Map();

    entityToAdd.UserId = User.GetUserId();

    entityToAdd.Date = DateTimeOffset.UtcNow;

    await DbContext.TodoItems.AddAsync(entityToAdd, cancellationToken);

    await DbContext.SaveChangesAsync(cancellationToken);

    return entityToAdd.Map();
}

[HttpPut]
public async Task<TodoItemDto> Update(TodoItemDto dto, CancellationToken cancellationToken)
{
    var entityToUpdate = await DbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken)
        ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);

    dto.Patch(entityToUpdate);

    await DbContext.SaveChangesAsync(cancellationToken);

    return entityToUpdate.Map();
}

[HttpDelete("{id}")]
public async Task Delete(Guid id, CancellationToken cancellationToken)
{
    DbContext.TodoItems.Remove(new() { Id = id });

    var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

    if (affectedRows < 1)
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ToDoItemCouldNotBeFound)]);
}
```

Notice how every async operation (`AddAsync`, `SaveChangesAsync`, `FirstOrDefaultAsync`) receives the `cancellationToken` parameter. This allows Entity Framework Core to cancel database operations if the user abandons the request.

---

### Client-Side Integration

#### Implementation

The cancellation token system works through a combination of server-side and client-side components:

**Server-side**: API methods accept `CancellationToken cancellationToken` parameter (as shown above).

**Client-side**: Components inherit from `AppComponentBase`, which provides a `CurrentCancellationToken` property. When making API calls, this token is automatically passed.

#### CurrentCancellationToken Property

Let's examine how `CurrentCancellationToken` is implemented in [`AppComponentBase.cs`](/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs):

```csharp
private CancellationTokenSource? cts = new();

protected CancellationToken CurrentCancellationToken
{
    get
    {
        if (cts == null)
            throw new OperationCanceledException(); // Component already disposed.
        cts.Token.ThrowIfCancellationRequested();
        return cts.Token;
    }
}
```

When the component is disposed (user navigates away, closes tab, etc.), the `CancellationTokenSource` is disposed, which automatically cancels all ongoing operations.

#### Client-Side Usage Example

Here's a real example from [`TodoPage.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/TodoPage.razor.cs):

```csharp
private async Task LoadTodoItems()
{
    allTodoItems = await todoItemController.Get(CurrentCancellationToken);
}

private async Task AddTodoItem()
{
    var addedTodoItem = await todoItemController.Create(new() { Title = newTodoTitle }, CurrentCancellationToken);
    // ... rest of the code
}

private async Task DeleteTodoItem()
{
    await todoItemController.Delete(deletingTodoItem.Id, CurrentCancellationToken);
    // ... rest of the code
}

private async Task SaveTodoItem(TodoItemDto todoItem)
{
    (await todoItemController.Update(todoItem, CurrentCancellationToken)).Patch(todoItem);
    // ... rest of the code
}
```

And in the Razor file ([`TodoPage.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/TodoPage.razor)), event handlers use `WrapHandled` for proper exception handling:

```xml
<BitTextField @ref="newTodoInput"
              Style="flex-grow:1"
              @bind-Value="newTodoTitle"
              Immediate DebounceTime="300"
              Placeholder="@Localizer[nameof(AppStrings.TodoAddPlaceholder)]"
              OnEnter="WrapHandled(async (KeyboardEventArgs args) => await AddTodoItem())" />

<BitButton AutoLoading
           OnClick="WrapHandled(AddTodoItem)"
           Title="@Localizer[nameof(AppStrings.Add)]"
           IsEnabled="(string.IsNullOrWhiteSpace(newTodoTitle) is false)">
    @Localizer[nameof(AppStrings.Add)]
</BitButton>
```

---

### User Abandonment Scenarios

#### Logical Cancellation

If a user clicks "Save" to update a Todo item and then **immediately**:

- Navigates to another page
- Closes the browser/app
- Performs any action that triggers navigation

The save operation is **automatically canceled**.

#### Why This is OK

The user didn't wait for the result, which could be:
- An error (e.g., validation failure, network error, duplicate product name)
- A success confirmation

Since they didn't wait, canceling the operation is the **logical behavior**. The user has already indicated (by navigating away) that they're no longer interested in the result.

---

## Navigation Lock for Critical Operations

### Purpose

For operations where you want to **prevent** automatic cancellation, use `NavigationLock`.

This is useful when:
- You're performing a critical operation that **must complete**
- You want to **warn the user** before they navigate away
- You need to **confirm** if the user really wants to leave
- You have unsaved changes that would be lost

**Important**: Use this only for **short critical operations**. For long-running tasks, use background jobs instead.

### Real-World Example

Let's look at [`AddOrEditCategoryModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor):

```xml
<NavigationLock OnBeforeInternalNavigation="OnNavigation" />
```

And the corresponding code-behind ([`AddOrEditCategoryModal.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor.cs)):

```csharp
private bool isChanged => editForm?.EditContext?.IsModified() is true;

private void OnNavigation(LocationChangingContext args)
{
    args.PreventNavigation();
    if (isChanged) return;
    isOpen = false;
}
```

**How it works:**
1. The `NavigationLock` component intercepts navigation attempts
2. If the form has unsaved changes (`isChanged` is true), navigation is **prevented**
3. The modal stays open, protecting the user from losing their work
4. If there are no changes, the modal is closed and navigation proceeds

This pattern is commonly used for:
- Edit forms with unsaved changes
- Multi-step wizards
- Critical data entry screens

---

## When to Use Background Jobs

### The Problem

What if the operation is **time-consuming**? For example:

- Sending an SMS with a verification code
- Generating a large report
- Processing uploaded files
- Sending bulk emails

**Users shouldn't have to:**
- Wait for the operation to complete
- Keep the page open
- Stay online until the task finishes

`NavigationLock` is **not appropriate** for long-running tasks because it forces users to wait.

### The Solution: Background Jobs with Hangfire

Instead of making the user wait, we **enqueue the task** and let it run in the background. The user can:
- Navigate away immediately
- Close their browser
- Come back later to check the result

**Key Benefits:**
- Operations are queued and processed asynchronously
- Server restarts or crashes don't lose the job
- Jobs are persisted in the database and automatically resume

---

## Background Job Implementation with Hangfire

### PhoneServiceJobsRunner Example

Let's examine how SMS sending is implemented as a background job.

#### Step 1: The Service Enqueues the Job

In [`PhoneService.cs`](/src/Server/Boilerplate.Server.Api/Services/PhoneService.cs):

```csharp
[AutoInject] private readonly IBackgroundJobClient backgroundJobClient = default!;

public virtual async Task SendSms(string messageText, string phoneNumber)
{
    if (hostEnvironment.IsDevelopment())
    {
        LogSendSms(phoneLogger, messageText, phoneNumber);
    }

    if (appSettings.Sms?.Configured is false) return;

    var from = appSettings.Sms!.FromPhoneNumber!;

    // Enqueue the job - this returns immediately
    backgroundJobClient.Enqueue<PhoneServiceJobsRunner>(x => x.SendSms(phoneNumber, from, messageText, default));
}
```

**Key points:**
- `backgroundJobClient.Enqueue<T>()` schedules the job to run in the background
- The method returns **immediately** - the user doesn't wait
- The `default` parameter is a placeholder for the `CancellationToken` (Hangfire will provide it)

#### Step 2: The Job Runner Executes the Task

In [`PhoneServiceJobsRunner.cs`](/src/Server/Boilerplate.Server.Api/Services/Jobs/PhoneServiceJobsRunner.cs):

```csharp
public partial class PhoneServiceJobsRunner
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30])]
    public async Task SendSms(string phoneNumber, string from, string messageText, CancellationToken cancellationToken)
    {
        try
        {
            var messageOptions = new CreateMessageOptions(new(phoneNumber))
            {
                From = new(from),
                Body = messageText
            };

            var smsMessage = MessageResource.Create(messageOptions);

            if (smsMessage.ErrorCode is not null)
                throw new InvalidOperationException(smsMessage.ErrorMessage)
                    .WithData(new() { { "Code", smsMessage.ErrorCode } });
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new() { { "PhoneNumber", phoneNumber } });
            
            if (exp is not KnownException && cancellationToken.IsCancellationRequested is false)
                throw; // To retry the job
        }
    }
}
```

**Key features:**

1. **AutomaticRetry**: If the job fails, Hangfire automatically retries it
   - `Attempts = 3`: Try up to 3 times
   - `DelaysInSeconds = [30]`: Wait 30 seconds between retries
   - This is perfect for SMS tokens that expire after 2 minutes

2. **Exception Handling**:
   - Logs the error with context (`PhoneNumber`)
   - For unknown exceptions, re-throws to trigger retry
   - For known exceptions (business logic errors), doesn't retry

3. **CancellationToken**: Even background jobs support cancellation (e.g., if the server is shutting down)

#### Step 3: Service Registration

In [`Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs):

```csharp
services.AddScoped<PhoneServiceJobsRunner>();
```

The job runner is registered as a scoped service so it has access to all the same dependencies as regular controllers (DbContext, IStringLocalizer, etc.).

---

### Key Benefits of Hangfire Integration

1. **Persistence**: Jobs are stored in the database
   - If the server crashes, jobs are not lost
   - When the server restarts, pending jobs are resumed

2. **Reliability**: Built-in retry mechanism
   - Transient failures (network issues, temporary service outages) are handled automatically
   - You can configure retry policies per job

3. **Scalability**: Jobs can be processed on different servers
   - Add more servers to process jobs faster
   - Background processing doesn't block web requests

4. **Monitoring**: Hangfire Dashboard
   - View all jobs (pending, processing, succeeded, failed)
   - Manually retry failed jobs
   - See job execution history and statistics

5. **Automatic Cleanup**: Old job records are automatically deleted
   - Keeps your database size manageable
   - Configurable retention policies

---

## Summary

In this stage, you learned:

✅ **CancellationToken** automatically cancels operations when users navigate away or close their browser  
✅ For API methods returning `IQueryable`, cancellation happens **implicitly** through OData and EF Core  
✅ `CurrentCancellationToken` in `AppComponentBase` provides automatic token management for Blazor components  
✅ `NavigationLock` prevents navigation when you have critical operations or unsaved changes (for **short operations only**)  
✅ **Background Jobs** with Hangfire allow long-running tasks without making users wait  
✅ `PhoneServiceJobsRunner` demonstrates a real-world background job implementation with retry logic  
✅ Hangfire provides **persistence, reliability, and scalability** for background tasks

---

## Best Practices

1. **Always pass CancellationToken**: Include `CancellationToken cancellationToken` in all async API methods
2. **Use CurrentCancellationToken**: In Blazor components, always use `CurrentCancellationToken` when calling APIs
3. **Use NavigationLock wisely**: Only prevent navigation when absolutely necessary (unsaved changes, **short** critical operations)
4. **Background jobs for long tasks**: If it takes more than a few seconds, consider using a background job
5. **Configure retry policies**: Use `[AutomaticRetry]` with appropriate attempt counts and delays for your use case
6. **Handle exceptions properly**: In background jobs, distinguish between transient errors (should retry) and business logic errors (shouldn't retry)

---