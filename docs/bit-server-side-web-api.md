# Bit Web API

Web API is a powerful library to develop rest api in .NET world. If you're not familiar with that, you can get start [here](https://www.asp.net/web-api).

Using bit you'll get more benefits from web api. This includes following:

1. We've configured web api on top of [owin](http://owin.org). Owin stands for "Open web interface for .NET" and allows your code to work almost anywhere. We've developed extra codes to make sure your app works well on following workloads:
    - ASP.NET/IIS on windows server & azure web/app services
    - ASP.NET Core/Kestrel on Windows & Linux Servers
    - Self host windows services & azure web jobs
2. We've configured web api on top of [asp.net core/owin branching](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware). Think about something as fast as node js with power of .NET (-:
3. We've developed roslyn analyzers to warn you about codes that won't work on ASP.NET Core.
4. We've developed extensive logging infrastructure in bit framework. It logs everything for you in your app, including web api traces.
5. We've configured headers like [X-Content-Type-Options](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options), [X-CorrelationId](http://theburningmonk.com/2015/05/a-consistent-approach-to-track-correlation-ids-through-microservices/) etc. We've done this to improve you logging, security etc.
6. You can protect your web api with bit identity server, a modern single sign on server based on open id/oauth 2.0

## Getting started

After installing [git for windows](https://git-scm.com/download/win) you can run following command in your command line: 
```shell
git clone https://github.com/bit-foundation/bit-framework.git
```

Then open Samples\WebApiSamples\WebApiSamples.sln, then go to 1SimpleWebApi project.

There are several classes there. Program and ValuesController are get copied from [this microsoft docs article](https://docs.microsoft.com/en-us/aspnet/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api). It's a good idea to read that article first.

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

That code configures web api into your app using default configuration. Default configuration is all about security, performance, logging etc.

Bit is a very extensible framework developed based on best practices. We've extensivly used dependency injection in our code base and you can customize default behaviors based on your requirements.

In following samples, you can findout how to customize web api in bit, but feel free to [drops us an issue in github](https://github.com/bit-foundation/bit-framework/issues), ask a question on [stackoverflow.com](http://stackoverflow.com/questions/tagged/bit-framework) or use comments below if you can't find what you want in these samples.

Note that security samples can be found under [Bit Identity Server](/bit-identity-server.md)

## Samples:

### Web API - Swagger configuration sample

Swagger is the World's Most Popular API Tooling. Using this sample you can findout how to customize web api in bit.

Read [first part of "Swagger and ASP.NET Web API"](http://wmpratt.com/swagger-and-asp-net-web-api-part-1/). We follow the second part in [Bit Identity Server](/bit-identity-server.md).

Differences between our sample (2WebApiSwagger project) and that article:

1- There is no App_Start folder. This is a ASP.NET thing.

2- In following code:

```csharp
GlobalConfiguration.Configuration
  .EnableSwagger(c => c.SingleApiVersion("v1", "A title for your API"))
  .EnableSwaggerUi();
```

[GlobalConfiguration](https://msdn.microsoft.com/en-us/library/system.web.http.globalconfiguration.aspx) uses ASP.NET pipeline directly and it does not work on ASP.NET Core. But bit's config works on both ASP.NET & ASP.NET Core.

3- Instead of adding Swashbuckle package, you've to install Swashbuckle.Core package. Swashbuckle package relys on ASP.NET pipeline and it does not work on ASP.NET Core. But our code works on both.

4- In following code:
```csharp
c.IncludeXmlComments(string.Format(@"{0}\bin\SwaggerDemoApi.XML",           
                           System.AppDomain.CurrentDomain.BaseDirectory));
```
We use #DefaultPathProvider.Current.GetCurrentAppPath()# instead of #System.AppDomain.CurrentDomain.BaseDirectory# which works fine on both ASP.NET/ASP.NET Core. We also use Path.Combine which works fine on linux servers instead of string.Format & \ charecter usage.

5- We've following code which has no equivalent in article codes:
```csharp
c.RootUrl(req => new Uri(req.RequestUri, req.GetOwinContext().Request.PathBase.Value /* /api */).ToString());
```
As you see in article, you open swgger ui by opening http://localhost:51609/swagger/ but in bit's sample you open http://localhost:9000/api/swagger/. You open /swagger under /api. This is a magic of owin/asp.net core's request branching. That improves your app performance a lot, because instead of passing all requests (even signalr and file requests) to swagger, you pass /api requests to swagger which is a expected behavior. That line of codes is telling where is swagger is hosted as it is not aware of that magic.

So run the second sample and you're good to go (-:

### Web API file upload sample

There is a [question](https://stackoverflow.com/questions/10320232/how-to-accept-a-file-post) on stackoverflow.com about web api file upload.
The important thing you've to notice is "You don't have to use System.Web.dll classes in bit world, even when you're hosting your app on traditional asp.net

By removing usages of that dll, you're going to make sure that your code works well on asp.net core too whenever you migrate your code (which can be done very easily using bit). So drop using #HttpContext.Current and all other members of System.Web.dll#

Web API Attribute routing works fine in bit project, but instead of [Route("api/file-manager/upload")] or [RoutePrefix("api/file-manager")], you've to write [Route("file-manager/upload")] or [RoutePrefix("file-manager)], this means you should not write /api in your attribute routings. That's a side effect of branching, which improves you're app performance in turn.

Remember to use async/await and CancellationToken in your Web API codes at it improves your app overall scability.

So open 3rd sample. It contains upload methods using Web API attribute routing. It uses async/await and CancellationToken and shows you how you can upload files to folders/database.

### Web API - Configuration on ASP.NET

In 4th project (4WebApiAspNetHost), you'll find a bit web api project hosted on ASP.NET/IIS.

Differences between this project and previews projects:

1- Instead of Microsoft.Owin.Host.HttpListener nuget package, we've installed Microsoft.Owin.Host.SystemWeb. Using first nuget packge, you can "self host" bit server side apps on windows services, console apps, azure job workers etc. Using second package, you can host bit server side apps on top of ASP.NET/IIS. All codes you've developed are the same (We've copied codes from 2WebApiSwagger project in fact).

Differences between this project and normal asp.net web api project:

1- There is a key to introduce AppStartup class as following:

```xml
<add key="owin:AppStartup" value="WebApiAspNetHost.AppStartup, WebApiAspNetHost" />
```

AppStartup is a class name & WebApiAspNetHost is a namespace. (Second one is assembly name)

2- To prevent asp.net web pages from being started, we've added following

```xml
<add key="webpages:Enabled" value="false" />
```

3- As we've introduced app startup class using "owin:AppStartup" config, we use following to disable automatic search:

```xml
<add key="owin:AutomaticAppStartup" value="false" />
```

4- We've added [enablePrefetchOptimization="true" optimizeCompilations="true"] to improve app overall performance.

5- We've removed all assemblies from being compiled as this is not required in bit projects:

```xml
<assemblies>
    <remove assembly="*" />
</assemblies>
```

6- We've added [enableVersionHeader="false"] to remove extra headers from responses.

7- We've removed all http handlers & modules as 1 owin handler manages everything for you.

```xml
<httpModules>
    <clear />
</httpModules>
<httpHandlers>
    <clear />
</httpHandlers>
```

8- We've disabled session as it is not required in bit projects.

```xml
<sessionState mode="Off" />
```

By following configs, we've removed extra modules and handlers

```xml
  <system.webServer>
    <validation validateIntegratedModeConfiguration="false" />
    <modules runAllManagedModulesForAllRequests="false">
      <remove name="Session" />
      <remove name="FormsAuthentication" />
      <remove name="DefaultAuthentication" />
      <remove name="RoleManager" />
      <remove name="FileAuthorization" />
      <remove name="AnonymousIdentification" />
      <remove name="Profile" />
      <remove name="UrlMappingsModule" />
      <remove name="ServiceModel-4.0" />
      <remove name="UrlRoutingModule-4.0" />
      <remove name="ScriptModule-4.0" />
      <remove name="WebDAVModule" />
    </modules>
    <defaultDocument>
      <files>
        <clear />
      </files>
    </defaultDocument>
    <handlers>
      <clear />
      <add name="Owin" verb="*" path="*" type="Microsoft.Owin.Host.SystemWeb.OwinHttpHandler, Microsoft.Owin.Host.SystemWeb" />
    </handlers>
    <httpProtocol>
      <customHeaders>
        <clear />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
```

### Web API - Configuration on ASP.NET Core

Comming soon.