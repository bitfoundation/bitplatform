# Bit Web API

Web API is a powerful library to develop rest api in .NET world. If you're not familiar with that, you can get start [here](https://www.asp.net/web-api).

Using bit you'll get more benefits from web api. This includes following:

1. We've configured web api on top of [owin](http://owin.org). Owin stands for "Open web interface for .NET". We've developed codes to make you're app up & running on following workloads:
    - ASP.NET/IIS on windows server & azure web/app services
    - ASP.NET Core/Kestrel on Windows & Linux Servers
    - Self host windows services & azure web workers
2. We've configured web api on top of [asp.net core/owin branching](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware). Think about something with performance of node js & power of .NET (-:
3. We've developed roslyn analyzers to warn you about codes that won't work on ASP.NET Core.
4. We've developed extensive logging infrastructure in bit framework. It logs everything for you in your app, including web api traces.
5. We've configured headers like [X-Content-Type-Options](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options), [X-CorrelationId](http://theburningmonk.com/2015/05/a-consistent-approach-to-track-correlation-ids-through-microservices/) etc. We've done this to improve you logging, security etc.
6. You can protect your web api with bit identity server, a modern single sign on server based on open id/oauth 2.0

## Getting started

After installing [git for windows](https://git-scm.com/download/win) you can run following command in your command line: git clone https://github.com/bit-foundation/bit-framework.git

Then open Samples\WebApiSamples\WebApiSamples.sln

There are several classes there. Program and ValuesController are get copied from[this microsoft docs article](https://docs.microsoft.com/en-us/aspnet/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api). It's a good idea to read that article first.

For now, lets ingore the third class: "AppStartup".

Press F5 and you'll see ["value1","value2"] at the end of console's output.

So, what's AppStartup class anyway? It configures your app. You'll understand its parts while you're reading docs, but for now let's focus on following codes of that only:

```csharp
dependencyManager.RegisterDefaultWebApiConfiguration();

dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
{
    return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
    {
        childDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration("WebApi");
    }).Resolve<IOwinMiddlewareConfiguration>("WebApi");

}, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
```

Bit is a very extensible framework developed based on best practices. We've extensivly used dependency injection in our code base and you can customize default behaviors based on your requirements.

That code configures web api into your app using default configuration. Default configuration is all about security, performance, logging etc. In FAQ, you can findout how to customize web api in bit, but if you've any other question, feel free to [drops us an issue in github](https://github.com/bit-foundation/bit-framework/issues). You can also ask a question on [stackoverflow.com](http://stackoverflow.com/questions/tagged/bit-framework), and you can use disqus comments below.

## Web API FAQ:

* How can I customize web api http configuration?
    Answer: Comming soon.
* How can I leverage dependency injection?
    Answer: Comming soon.

## Other samples:

### Web API file upload/download sample

Using this sample, you'll findout several important concepts, from async/await to ASP.NET Core friendly development.

Comming soon.

### Secure Web API sample

This sample is a web api which is protected by bit identity server.

Comming soon.