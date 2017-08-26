# Bit server side Introduction

You can develop server side of your apps using Bit framework.

Your app can be hosted on [Traditional ASP.NET](https://www.asp.net/) or [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) \(With [Full .NET Framework](https://www.microsoft.com/net)\). ASP.NET Core with [.NET Core](https://www.microsoft.com/net/core) support will be added in near future. You can track that by following [this issue.](https://github.com/bit-foundation/bit-framework/issues/59)

By the abstraction we provide to you, you develop no matter you choose ASP.NET Core, ASP.NET Traditional or ASP.NET Core with .NET Core. You develop on ASP.NET Core the same way as you develop on ASP.NET Traditional. Only configurations between those platforms are different.

> **[success]Recommendation:**
>
> Choose ASP.NET Core with Full .NET Framework. After we supported .NET Core, you can upgrade your app to .NET Core easily. Note that not all apps should use .NET Core, due its limitations. As an example, Entity framework 6 (Which is not availble in .NET Core) has several important features you can't find in Entity framework core.

We also watch your codes at realtime to inform you about common mistakes. For example, if you use some classes which are not part of ASP.NET Core / .NET Core, you'll see something like this:

![](/assets/WarnAboutNonASPNETCoreCompatilbeCodeUsage.png)
