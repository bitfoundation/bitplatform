# Cloud and On-Premise Log Solution

\#\# Cloud and On-Premise Log Solution [**Application Insights**](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) is a solution provided by Microsoft that helps us to have an effective and efficient log system in three sections:

1- Basic log methods that are called **manually** that go beyond the usual logger methods, such as [TrackEvent](https://docs.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient.trackevent?view=azure-dotnet) method to log a business event.

2- Application Insights **automatically** logs system errors and also the length of time it takes to work with the Http Client, database, and other dependencies, along with the `request address` or `SQL command text` and other useful information which leads to the storage of very valuable data in the system. Of course, if a dependency is not detected automatically, like Redis, you can deliver its info to AppInsights yourself using the [TrackDependency](https://docs.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient.trackdependency?view=azure-dotnet) method.

3- Azure's App Insights **dashboard** allows you to search logs as quickly as possible, for example, to see all the works done by a particular user or if the user wants to export Excel from the product list and it takes 1 second, how long is it waiting for the database and ... In addition, you can use Power BI to extract important points from logs.

Of course, App Insights may not seem to be suitable for those who do not have an Azure account, but what if there is a solution for storing log information on-premise? For example, if it saves its information in Elastic on the company's servers, without the need for that server to even have Internet access.

Yes, it is possible, and with the help of [Microsoft Diagnostics EventFlow](https://github.com/Azure/diagnostics-eventflow), App Insights information can be stored anywhere, including Elastic, and thus the major benefits of App Insights are available without having an Azure account.

To do this, follow the instructions: _\(The tutorial is for ASP.NET Core 3.1, but can be used for other projects as well\)_

1- First add Application Insights to your project. To do this, you need to install `Microsoft.Extensions.Logging.ApplicationInsights` and `Microsoft.ApplicationInsights.AspNetCore` packages.

2- In `Program.cs` after:

```csharp
Host.CreateDefaultBuilder(args)
```

Enter the below code:

```csharp
.ConfigureLogging(loggingBuilder  =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddApplicationInsights();
})
```

This will remove the default `Console` and `Debug` loggers, and of course if a 3rd party library has used `Microsoft.Extensions.Logging`, the log information will also be given to AppInsights, and finally, with the help of [Microsoft Diagnostics EventFlow](https://github.com/Azure/diagnostics-eventflow), you will have that information in Elastic, Console, and other outputs.

3- If you are going to log an **event** somewhere, or in the example of using Redis you want to log information about the response time from Redis or you have a try/catch block in which you do not throw the error again, but you want to log the exception, first inject [TelemetryClient](https://docs.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient?view=azure-dotnet) and then use its methods such as `TrackException`. Note that it will also work if you use the `ILogger` provided by `Microsoft.Extensions.Logging`.

4- Install [Microsoft.Diagnostics.EventFlow.Inputs.ApplicationInsights](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.ApplicationInsights/) package in the project and then select one of the introduced outputs and install its package as well. You can send the data that AppInsights automatically collects + the data provided by you to Elastic, Splunk, etc. In this example, **for the sake of simplilcity**, we select `StdOut - Console Output` and install the `Microsoft.Diagnostics.EventFlow.Outputs.StdOutput` package.

5- Add the `eventFlowConfig.json` file to the project as below:

```javascript
{
  "inputs": [
    {
      "type": "ApplicationInsights"
    }
  ],
  "outputs": [
    {
      "type": "StdOutput" // console output
    }
  ],
  "schemaVersion": "2016-08-11"
}
```

In this file, inputs include ApplicationInsights, and of course, you can include interesting items such as `PerformanceCounters` in it, and the outputs can be Elastic, depending on your needs, in which case the connection config info to Elastic server should be provided. Note that we are using the ApplicationInsights SDK as a logging library, and eventually the data is sent not to ApplicationInsights on Microsoft Azure servers, but to the output or outputs configured in the eventFlowConfig.json file.

6- In `Program.cs`, modify the `Main` method as follows:

```csharp
using (var pipeline = DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
{
    CreateHostBuilder(args, pipeline)
        .Build()
        .Run();
}
```

And `CreateHostBuilder` method should look like this:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args, DiagnosticPipeline pipeline) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices(services => services.AddSingleton<ITelemetryProcessorFactory>(sp => new EventFlowTelemetryProcessorFactory(pipeline)))
        .ConfigureLogging(logginBuilder =>
        {
            logginBuilder.ClearProviders();
            loggingBuilder.AddApplicationInsights();
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

Everything is ready. Now if you have an Azure account, you can use the great ApplicationInsights dashboard by giving `instrumentationKey` in `appsetting.json`, and if not, you can still launch Elastic, Splunk, etc on your internal servers and get the detailed and functional logs that were automatically collected along with the information that you provided manually by configuring the output\(s\) connection information in the `outputs` section of `eventFlowConfig.json` file.

You can see the [sample project](https://github.com/ysmoradi/AppInsightsSdkWithoutAzureAccount) that contains the Elastic example on GitHub.

