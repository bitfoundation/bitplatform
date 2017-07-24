# Bit server side Introduction

You can develop server side of your apps using Bit framework.

Your app can be hosted on [Traditional ASP.NET](https://www.asp.net/) or [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) \(With [Full .NET Framework](https://www.microsoft.com/net)\). ASP.NET Core with [.NET Core](https://www.microsoft.com/net/core) support will be added in near future. You can track that by following [this issue.](https://github.com/bit-foundation/bit-framework/issues/59)

By the abstraction we provide to you, your develop no matter you choose ASP.NET Core, ASP.NET Traditional or ASP.NET Core with .NET Core. You develop on ASP.NET Core the same way as you develop on ASP.NET Traditional. Only configurations between those platforms are different.

> **[info] Our recommendation:**
> Choose ASP.NET Core with Full .NET Framework. You can host your app on Windows/Linux servers + You can use powerful libraries like Entity Framework Full \(6\). As .NET > Core becomes mature, you can upgrade your app to .NET Core easily.

We've some C\# analyzers to detect your codes which are not compatible with ASP.NET Core \| .NET Core. You'll see something like this:

![](/assets/WarnAboutNonASPNETCoreCompatilbeCodeUsage.png)

**What we provide to you at server side:**

* API-OData
* Single sign on server
* Background Job Worker
* Real time communication
* Data access
* Test framework for your server side codes
* Integrated Logging infrastructure
* , ...

Remember that you can use whatever you want. You're not limited to what we provide to you.
