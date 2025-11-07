# Stage 4: Background Jobs and CancellationToken Management

Welcome to Stage 4! In this stage, you'll learn how the project handles **request cancellation** and **background job processing** to ensure efficient resource management and reliable execution of long-running tasks.

---

## Table of Contents

1. [CancellationToken in API Requests](#cancellationtoken-in-api-requests)
2. [Client-Side Integration](#client-side-integration)
3. [User Abandonment Scenarios](#user-abandonment-scenarios)
4. [Navigation Lock for Critical Operations](#navigation-lock-for-critical-operations)
5. [When to Use Background Jobs](#when-to-use-background-jobs)
6. [Background Job Implementation with Hangfire](#background-job-implementation-with-hangfire)
7. [Real-World Example: PhoneServiceJobsRunner](#real-world-example-phoneservicejobsrunner)
8. [Key Benefits](#key-benefits)

---

## CancellationToken in API Requests

### Automatic Request Cancellation

All API methods in this project receive a `CancellationToken` parameter that **automatically cancels** operations when:

- **The user navigates away** from the current page
- **The browser/app is closed** by the user
- **The component is disposed** (unmounted from the UI)
- **A new request is initiated** that supersedes the previous one
- **The server detects** that the client has disconnected

This automatic cancellation **prevents wasted server resources** on operations whose results will never be consumed.

---

## Client-Side Integration

### How CurrentCancellationToken Works

The cancellation system works through a combination of:

**Server-side**: API methods accept `CancellationToken cancellationToken` parameter:

```csharp
// File: src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs
[HttpGet("{id}")]
public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
{
    var product = await DbContext.Products
        .Where(p => p.Id == id)
        .Project()
        .FirstOrDefaultAsync(cancellationToken) 
        ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductNotFound)]);

    return product;
}
```

**Client-side**: Components inherit `CurrentCancellationToken` from `AppComponentBase` and pass it when making API calls:

```csharp
// File: src/Client/Boilerplate.Client.Core/Components/Pages/Products/AddOrEditProductPage.razor.cs
protected override async Task OnParamsSetAsync()
{
    await base.OnParamsSetAsync();

    if (Id != null)
    {
        // CurrentCancellationToken is automatically managed by AppComponentBase
        // It will be cancelled if the user navigates away or the component is disposed
        product = await productController.Get(Id.Value, CurrentCancellationToken);
    }
}
```

### How AppComponentBase Manages Cancellation

The `AppComponentBase` class (located in [`src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`](../src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)) provides the `CurrentCancellationToken` property:

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

public async ValueTask DisposeAsync()
{
    if (cts != null)
    {
        using var currentCts = cts;
        cts = null;
        await currentCts.CancelAsync(); // ✅ Automatically cancels all ongoing operations
    }
    // ... other disposal logic
}
```

**Key Points:**
- Each component has its own `CancellationTokenSource`
- When the component is disposed (user navigates away), all pending operations are automatically cancelled
- This prevents memory leaks and wasted server resources

---

## User Abandonment Scenarios

### Logical Cancellation in Action

Consider this scenario: A user clicks "Save" to update a Product and then **immediately**:

- **Navigates to another page**
- **Closes the browser/app**
- **Clicks elsewhere in the UI**

In this case, the save operation is **automatically canceled**.

### Why This Behavior Makes Sense

**The user didn't wait for the result**, which could be:
- ✅ A successful save
- ❌ An error (e.g., duplicate product name, validation failure)

Since they didn't wait, **canceling the operation is the logical behavior**:
- The user clearly didn't care about the result
- Server resources are freed immediately
- Database locks are released faster
- No unnecessary processing occurs

---

## Navigation Lock for Critical Operations

### What is NavigationLock?

For operations where you want to **prevent** automatic cancellation, use Blazor's `NavigationLock` component:

- **Prompts the user** to confirm before navigating away
- **Useful for short critical operations** where accidental navigation would be problematic
- **Example use case**: Saving important form data

### Real Example from the Project

**File**: [`src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor`](../src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor)

```xml
@if (isOpen)
{
    <NavigationLock OnBeforeInternalNavigation="OnNavigation" />
}

<BitModal @bind-IsOpen="isOpen"
          AutoToggleScroll="false" 
          Blocking="isChanged"
          Draggable DragElementSelector=".header-stack">
    <EditForm Model="category" OnValidSubmit="WrapHandled(Save)" novalidate>
        <!-- Form fields here -->
    </EditForm>
</BitModal>
```

**Code-behind**: [`src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor.cs`](../src/Client/Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor.cs)

```csharp
private bool isChanged => editForm?.EditContext?.IsModified() is true;

private async Task OnNavigation(LocationChangingContext context)
{
    if (isChanged)
    {
        await ConfirmMessageBox.Show(
            message: Localizer[nameof(AppStrings.AreYouSureYouWantToLeaveThisPage)],
            title: Localizer[nameof(AppStrings.UnsavedChanges)]);
    }
}
```

**How it works:**
1. `NavigationLock` detects when the user tries to navigate away
2. If the form has unsaved changes (`isChanged == true`), it prompts the user
3. The user can choose to stay or leave
4. This prevents accidental data loss

---

## When to Use Background Jobs

### The Problem with Long-Running Operations

What if the operation is **time-consuming**? Examples:
- **Sending SMS messages** (network latency, external API calls)
- **Processing large files** (image resizing, PDF generation)
- **Generating reports** (complex database queries, aggregations)
- **Sending bulk emails** (hundreds or thousands of recipients)

**Problems with synchronous execution:**
- ❌ Users shouldn't have to **wait and keep the page open**
- ❌ If the user closes the browser, **the operation is lost**
- ❌ Server resources are tied up during the entire operation
- ❌ Browser/app may time out waiting for the response

**NavigationLock is NOT appropriate** for long-running tasks because:
- Users don't want to wait 30+ seconds
- They might accidentally close the browser
- Network interruptions would fail the entire operation

### The Solution: Background Jobs with Hangfire

Use **Hangfire** to queue long-running operations:

✅ **Operations are queued** and processed asynchronously  
✅ **Server restarts or crashes** don't lose the job  
✅ **Jobs are persisted** in the database and automatically resume  
✅ **Automatic retries** with configurable policies  
✅ **Users get immediate feedback** and can continue using the app  
✅ **Scalable**: Jobs can be processed by multiple servers  

---

## Background Job Implementation with Hangfire

### Hangfire Setup in the Project

**Registration**: [`src/Server/Boilerplate.Server.Api/Program.Services.cs`](../src/Server/Boilerplate.Server.Api/Program.Services.cs)

```csharp
// Register background job services
services.AddScoped<PhoneServiceJobsRunner>();
services.AddScoped<EmailServiceJobsRunner>();

// Configure Hangfire
builder.Services.AddHangfire(configuration =>
{
    configuration.UseEFCoreStorage(
        () => builder.Services.BuildServiceProvider().GetRequiredService<AppDbContext>(),
        new() { SchemaName = "jobs" }
    );
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.ServerName = $"{Environment.MachineName}:{Environment.ProcessId}";
});
```

### Hangfire Dashboard

Hangfire provides a **built-in dashboard** to monitor jobs:

- **URL**: `/hangfire` (requires authentication)
- **View pending, processing, succeeded, and failed jobs**
- **Retry failed jobs manually**
- **Monitor job execution times and performance**

---

## Real-World Example: PhoneServiceJobsRunner

Let's explore a complete example of how background jobs are used in the project.

### Step 1: The Service Layer (PhoneService)

**File**: [`src/Server/Boilerplate.Server.Api/Services/PhoneService.cs`](../src/Server/Boilerplate.Server.Api/Services/PhoneService.cs)

```csharp
public partial class PhoneService
{
    [AutoInject] private readonly IBackgroundJobClient backgroundJobClient = default!;

    public virtual async Task SendSms(string messageText, string phoneNumber)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSendSms(phoneLogger, messageText, phoneNumber);
        }

        if (appSettings.Sms?.Configured is false) return;

        var from = appSettings.Sms!.FromPhoneNumber!;

        // ✅ Instead of sending SMS immediately, QUEUE it as a background job
        backgroundJobClient.Enqueue<PhoneServiceJobsRunner>(x => 
            x.SendSms(phoneNumber, from, messageText, default));
    }
}
```

**Key Points:**
- The `SendSms` method **returns immediately** after queueing the job
- The actual SMS sending happens **asynchronously** in the background
- The user doesn't have to wait for the external SMS API to respond
- In development mode, it logs instead of actually sending

### Step 2: The Background Job Runner

**File**: [`src/Server/Boilerplate.Server.Api/Services/Jobs/PhoneServiceJobsRunner.cs`](../src/Server/Boilerplate.Server.Api/Services/Jobs/PhoneServiceJobsRunner.cs)

```csharp
public partial class PhoneServiceJobsRunner
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    [AutomaticRetry(
        Attempts = 3, 
        DelaysInSeconds = [30] /* Retry 3 times with 30 seconds between attempts */
    )]
    public async Task SendSms(
        string phoneNumber, 
        string from, 
        string messageText, 
        CancellationToken cancellationToken)
    {
        try
        {
            var messageOptions = new CreateMessageOptions(new(phoneNumber))
            {
                From = new(from),
                Body = messageText
            };

            // Call external Twilio SMS API
            var smsMessage = MessageResource.Create(messageOptions);

            if (smsMessage.ErrorCode is not null)
                throw new InvalidOperationException(smsMessage.ErrorMessage)
                    .WithData(new() { { "Code", smsMessage.ErrorCode } });
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new() { { "PhoneNumber", phoneNumber } });
            
            if (exp is not KnownException && cancellationToken.IsCancellationRequested is false)
                throw; // ✅ Re-throw to trigger Hangfire's automatic retry
        }
    }
}
```

**Key Features:**

1. **`[AutomaticRetry]` Attribute**:
   - Hangfire will **automatically retry** failed jobs
   - `Attempts = 3`: Try up to 3 times
   - `DelaysInSeconds = [30]`: Wait 30 seconds between retries
   - **Why limited retries?** SMS tokens typically expire after 2 minutes, so excessive retries are pointless

2. **Exception Handling**:
   - Logs the error with context (`PhoneNumber`)
   - Re-throws **unknown exceptions** to trigger retry
   - Swallows **known exceptions** to prevent unnecessary retries

3. **Cancellation Support**:
   - Accepts `CancellationToken` for graceful shutdown
   - If the job is cancelled (e.g., server shutdown), it won't retry

### Step 3: Usage in Controllers

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.PhoneConfirmation.cs`](../src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.PhoneConfirmation.cs)

```csharp
private async Task SendConfirmPhoneToken(User user, CancellationToken cancellationToken)
{
    var phoneNumber = user.PhoneNumber!;
    var token = await userManager.GenerateUserTokenAsync(
        user, 
        TokenOptions.DefaultPhoneProvider, 
        FormattableString.Invariant($"VerifyPhoneNumber:{phoneNumber},{user.PhoneNumberTokenRequestedOn?.ToUniversalTime()}")
    );

    var message = Localizer[nameof(AppStrings.ConfirmPhoneTokenShortText), token];
    var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}"; // Web OTP format

    // ✅ Queues the SMS as a background job and returns immediately
    await phoneService.SendSms(smsMessage, phoneNumber);
}
```

**Benefits:**
- The API endpoint returns **immediately** after queuing the job
- User doesn't wait for the external SMS API
- If Twilio is slow or fails, the job will retry automatically
- Server restarts won't lose queued SMS messages

---

## Key Benefits

### 1. **Persistence**
Jobs are stored in the database (schema: `jobs`). Even if the server crashes or restarts, jobs will resume automatically.

**Database Tables (Created by Hangfire)**:
- `HangfireJob`: Job metadata
- `HangfireState`: Job state history (Enqueued → Processing → Succeeded/Failed)
- `HangfireJobParameter`: Job parameters
- `HangfireQueuedJob`: Jobs waiting to be processed

### 2. **Reliability**
- Automatic retries with configurable delays
- Failed jobs can be retried manually via the dashboard
- No jobs are lost even in failure scenarios

### 3. **Scalability**
- Jobs can be processed on **different servers**
- Worker count is configurable: `WorkerCount = Environment.ProcessorCount * 2`
- Supports distributed processing for high-load scenarios

### 4. **Observability**
- Built-in dashboard at `/hangfire`
- View job execution times, success/failure rates
- Monitor queue lengths and worker utilization

### 5. **User Experience**
- Users get **instant feedback** instead of waiting
- No timeouts or connection issues
- App remains responsive during long operations

---

## Summary

In this stage, you learned:

✅ **CancellationToken**: How API requests are automatically cancelled when users navigate away  
✅ **CurrentCancellationToken**: How `AppComponentBase` manages cancellation for components  
✅ **User Abandonment**: Why automatic cancellation makes sense for abandoned operations  
✅ **NavigationLock**: How to prevent navigation for critical short operations  
✅ **Background Jobs**: When and why to use Hangfire for long-running tasks  
✅ **PhoneServiceJobsRunner**: A real-world example with automatic retries and error handling  
✅ **Benefits**: Persistence, reliability, scalability, and better user experience  

---

## Additional Resources

- **Hangfire Documentation**: [https://docs.hangfire.io](https://docs.hangfire.io)
- **CancellationToken Best Practices**: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
- **Blazor NavigationLock**: [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing#navigationlock-component)
