# Optimized entity platform for N-Tier applications

Using Entity Framework with "ASP.NET/ASP.NET Core" "Web API/MVC" is a common thing in .NET based projects. Most developers use entity framework with its default configuration. Entity framework has several features such as **Automatic** Property based Lazy loading, **Automatic** change tracking etc. But every feature comes with a cost and unfortunately these features are not useful in most scenarios in N-Tier apps.

Consider a web api which returns list of active customers from the database. With or without a repository, an entity framework's db context gets created. Then it runs a query against a database, creates an instance of the customer for each record of database result set. Then db context gets disposed. After that, [Json.NET](http://www.newtonsoft.com/json) returns json created from customers list.

When user/operator changes any of those customers in a web app or mobile app, we send changes to the server and a "new" instance of customer gets created based on that. We save that customer using "newly" created db context. So in this sample, there is no need to automatic change tracking to get active customers first, because changes are not applied on that specific customers & db context instances. Changes are made in separate tier.

This sample is a common thing in N-Tier world, when **most changes are made in other tiers such as browsers and mobile devices**.

In bit platform, we disable entity framework features by default. Features like "Property based Lazy loading \(The lazy loading itself works properly\)", "Automatic Change Tracking", "Proxy creation" etc. Bit repository, on the other hand, calls "AsNoTracking" on all your queries.

We've developed roslyn analyzers to inform you when you're doing something wrong while you're working with db context too. See below:

![](/.gitbook/assets/EntityFrameworkAsNoTrackingRoslynAnalyzer.PNG)

[You can see what we've done to entity framework's configuration here](https://github.com/bitfoundation/bitplatform/blob/master/src/Server/Bit.Server.Data.EntityFramework/Implementations/EfDbContextBase.cs#L70-L76)

But when you apply those configurations, most repositories won't work properly as they are developed/tested mostly based on default entity framework's configuration. This is why we developed a new repository in bit platform instead of using any of existing repository libraries. \(It has other important reasons although, such as true async support etc\)

You're free to use your preferred repository, but let's take a look at some benchmarks: [You can find codes here](https://github.com/bitfoundation/bitplatform/tree/master/docs/src/EntityFrameworkOptimizedForNTierScenarios)

```text
Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host] : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```

|          Method |        Mean |       Error |      StdDev |
|---------------- |------------:|------------:|------------:|
|   BitRepository | 53,829.7 us | 1,036.41 us | 1,996.80 us |
| SharpRepository | 54,342.4 us | 1,075.09 us | 1,359.64 us |
|       EmptyList |    195.4 us |     0.67 us |     0.62 us |

[This script](https://github.com/bitfoundation/bitplatform/blob/master/docs/src/EntityFrameworkOptimizedForNTierScenarios/CreateTestDatabaseScript.sql) creates a database which has 10.000 customers, and each customer has 3 orders. As you can see in benchmarks, returning empty list is very fast. It's all about **micro seconds**. BitRepository has Mean with value \(20.00 ms\) which is acceptable as it's returning whole 10.000 customers in every request. And finally, you see Sharp repository's result which is about seconds! "It's not because of SharpRepository itself", it is because of the default configuration of entity framework.

Default entity framework configuration slows down your app because of its proxy creation \(required for property based lazy loading\). Proxy creation is a slow process. Property based lazy loading slows down your app too, because when you return 10 customers to a client, it sends 10 queries to the database to retrieve their orders! This is a known problem called **N+1 problem**. Automatic change tracking which is not useful in this scenario is another reason for slowness.

Ninja tip: [Here you can learn how to do lazy loading withour slowing down your app + support for async lazy loading!](https://docs.bitplatform.dev/docs/bit-server-side/data-access.html#bit-repository-specific-methods)

