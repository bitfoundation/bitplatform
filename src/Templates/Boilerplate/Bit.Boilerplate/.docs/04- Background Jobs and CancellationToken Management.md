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
        // ...
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

### Preventing Navigation During Save

Let's look at a example of how `NavigationLock` can be used to prevent navigation while a save operation is in progress.

#### Razor Component (`.razor` file)

```xml
@inherits AppPageBase

<NavigationLock OnBeforeInternalNavigation="HandleNavigation" />

<EditForm Model="product" OnValidSubmit="WrapHandled(SaveProduct)">
    <BitTextField @bind-Value="product.Name" Label="Product Name" />
    <BitTextField @bind-Value="product.Price" Label="Price" />
    
    <BitButton IsLoading="isSaving" ButtonType="BitButtonType.Submit">
        @Localizer[nameof(AppStrings.Save)]
    </BitButton>
</EditForm>
```

#### Code-Behind (`.razor.cs` file)

```csharp
public partial class EditProductPage
{
    [AutoInject] IProductController productController = default!;
    
    private bool isSaving = false;
    private ProductDto product = new();

    private async Task SaveProduct()
    {
        if (isSaving) return; // Prevent multiple submissions

        isSaving = true; // Set flag BEFORE starting the operation

        try
        {
            // This operation must complete - don't allow navigation
            await productController.Update(product, CurrentCancellationToken);
            
            SnackBarService.Success("Product saved successfully!");
        }
        finally
        {
            isSaving = false; // Always reset the flag
        }
    }

    private void HandleNavigation(LocationChangingContext context)
    {
        // If save operation is in progress, prevent navigation
        if (isSaving)
        {
            context.PreventNavigation();
        }
    }
}
```

**How it works:**

1. **NavigationLock Component**: `<NavigationLock OnBeforeInternalNavigation="HandleNavigation" />` is rendered when the modal/page is open
2. **isSaving Flag**: Set to `true` when the save operation starts
3. **HandleNavigation Method**: Called by `NavigationLock` before any navigation occurs
   - If `isSaving` is `true`, it calls `context.PreventNavigation()` to block navigation
   - This ensures the save operation completes before the user can leave
4. **User Experience**: The user sees a loading indicator on the button (`IsLoading="isSaving"`) and cannot navigate away until the operation completes

**When Navigation is Blocked:**
- User clicks browser back button → **Blocked**
- User clicks a menu item → **Blocked**
- User refreshes the page → **Browser's native "Leave site?" dialog appears**

**When Navigation is Allowed:**
- After `isSaving` is set back to `false` in the `finally` block
- The operation completes (success or failure) and the flag is reset

### Usage Patterns

This pattern can be used for:
- Edit forms with unsaved changes
- Multi-step wizards
- Critical data entry screens
- Payment processing forms
- Any operation that must not be interrupted

---

## When to Use Background Jobs

### The Problem

What if the operation is **time-consuming**? For example:

- Sending an SMS with a verification code
- Generating a large pdf report
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
    backgroundJobClient.Enqueue<PhoneServiceJobsRunner>(x => x.SendSms(phoneNumber, from, messageText));
}
```

**Key points:**
- `backgroundJobClient.Enqueue<T>()` schedules the job to run in the background
- The method returns **immediately** - the user doesn't wait
- Hangfire will automatically provide the `PerformContext` and `CancellationToken` parameters to the job runner method

#### Step 2: The Job Runner Executes the Task

In [`PhoneServiceJobsRunner.cs`](/src/Server/Boilerplate.Server.Api/Services/Jobs/PhoneServiceJobsRunner.cs):

```csharp
public partial class PhoneServiceJobsRunner
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30])]
    public async Task SendSms(string phoneNumber, string from, string messageText,
        PerformContext context = null!,
        CancellationToken cancellationToken = default)
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
                throw new InvalidOperationException(smsMessage.ErrorMessage).WithData(new() { { "Code", smsMessage.ErrorCode } });
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new()
            {
                { "PhoneNumber", phoneNumber },
                { "JobId", context.BackgroundJob.Id }
            });
            if (exp is not KnownException && cancellationToken.IsCancellationRequested is false)
                throw; // To retry the job
        }
**Key features:**

1. **AutomaticRetry**: If the job fails, Hangfire automatically retries it
   - `Attempts = 3`: Try up to 3 times
   - `DelaysInSeconds = [30]`: Wait 30 seconds between retries
   - This is perfect for SMS tokens that expire after 2 minutes

2. **Hangfire-Provided Parameters**: The method accepts two special parameters that Hangfire automatically provides:
   - `PerformContext context`: Provides access to job metadata (like `JobId`, `BackgroundJob`, etc.)
   - `CancellationToken cancellationToken`: Signals when the job should be cancelled (e.g., server shutdown)
   - These parameters have default values (`null!` and `default`) so Hangfire can invoke the method

3. **Exception Handling**:
   - Logs the error with context (`PhoneNumber`, `JobId`)
   - For unknown exceptions, re-throws to trigger retry
   - For known exceptions (business logic errors), doesn't retry

4. **CancellationToken**: Even background jobs support cancellation (e.g., if the server is shutting down)

**Important**: Inside background job, there is **NO** `IHttpContextAccessor` or `User` object available. So if user context is needed, it must be passed as parameters to the job method.

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

### AI Wiki: Answered Questions
* [How does the Abort() method in AppComponentBase differ from component disposal, and when should developers explicitly call it versus relying on the DisposeAsync lifecycle?](https://deepwiki.com/search/how-does-the-abort-method-in-a_187b27a3-091b-4e36-9905-0bf5b128b4aa)

Ask your own question [here](https://wiki.bitplatform.dev)

---