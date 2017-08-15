# Bit Data Access

You can use Bit Data Access components, or you can use your own preferred way to read/manipulate data. But why you might choose Bit data access anyway?

1- True async support. Application with async code gets scaled better, but what does this mean to us? Each server side app has limited threads. Threads are workers and your app works because they work. Threads count is limited, so you've to use them carefully. When a request comes to your web api action, and you get data from database using entity framework (For example), your thread (your valuable thread) waits until database returns data. But that wait is useless. In case you use async-await, your thread executes other requests instead of waiting for a database.

2- True cancellation token support. There is a CancellationToken in every web api action you develop. If user/operator closes its browser, or if you cancel request at client side programmatically, that cancellation token gets notified. Almost all bit framework's methods accept cancellation token, and they stop their work by simply passing that token.

3- Bit Data Access components are optimized for N-Tier app development. To have a better understanding about what does this mean read this [amazing article](https://docs.bit-framework.com/docs/design-backgrounds/optimized-entity-framework-for-n-tier-apps.html).

### Entity Framework

Getting started: (Sample can be found [here](https://github.com/bit-foundation/bit-framework/tree/master/Samples/DataAccessSamples/))

At first, you've to develop your domain model. Use IEntity interface to mark your classes as entity. It's just a marker and has no member to implement really.

```csharp
public class Customer : IEntity
{
}
```

Then develop a DbContext class which inherits from EfDbContextBase. The reason is described [here](https://docs.bit-framework.com/docs/design-backgrounds/optimized-entity-framework-for-n-tier-apps.html).

```csharp
public class MyAppDbContext : EfDbContextBase
{
    public MyAppDbContext()
        : base(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"))
    {
        // This constructor is need for migrations/initializers etc.
    }

    public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbConnectionProvider dbConnectionProvider)
        : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbConnectionProvider)
    {
        // This constructor has better performance, provides implicit unit of work, etc. And it is automatically used by bit framework while processing requests etc.
    }

    public virtual DbSet<Customer> Customers { get; set; }
}
```

App environment provider provides you a way to access your configuration. By default, it reads configs from environments.json file. You don't have to use that, your db context just needs a connection string, read it from app environment provider, asp.net core's configuration provider, app/web config files or write connection string there directly.

See environments.json file

```json
"Configs": [
    {
        "Key": "AppConnectionString",
        "Value": "Data Source=.;Initial Catalog=MyAppDb;Integrated Security=True;"
    }
]
```

Then create a repository class for your project as following:

```csharp
public class MyAppRepository<TEntity> : EfRepository<TEntity>
    where TEntity : class, IEntity
{

}

```

In Web API classes, use that as following:

```csharp
public class CustomersController : ApiController
{
    public virtual IRepository<Customer> CustomersRepository { get; set; }

    [Route("customers/get-customers")]
    public virtual async Task<List<Customer>> GetCustomers(CancellationToken cancellationToken)
    {
        return await (await CustomersRepository.GetAllAsync(cancellationToken)).ToListAsync(cancellationToken);
    }

    public virtual async Task AddNewCustomer(CancellationToken cancellationToken, Customer customer)
    {
        await CustomersRepository.AddAsync(customer, cancellationToken);
        // App has implicit unit of work. If you throw an exception here, we save nothing to database
    }
}
```

To develop a custom repository, use following codes:

```csharp
public interface IOrdersRepository : IRepository<Order>
{
    Task<long> GetOrdersCount(CancellationToken cancellationToken);
}

public class OrdersRepository : MyAppRepository<Order>, IOrdersRepository
{
    public virtual async Task<long> GetOrdersCount(CancellationToken cancellationToken)
    {
        // Note that GetAllAsync returns you a query, it does not return all data. So following code has this sql as its equivalent: select count_big(*) from Orders
        return await (await GetAllAsync(cancellationToken)).LongCountAsync(cancellationToken);
    }
}
```

You can use app events to do something at app startup/end. You can initialize your db context as you see in MyAppDbContextInitializer class.

Then register followings:

```csharp
dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>(); // Uses Sql connection
dependencyManager.RegisterEfDbContext<MyAppDbContext>(); // Registers db context class
dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>(); // App event to initialize db context at startup
dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(MyAppRepository<>).GetTypeInfo()); // You can inject IRepository<Customer> or IRepository<any class you want> by this generic registrations
dependencyManager.Register<IOrdersRepository, OrdersRepository>(); // It registers custome orders repository
```

### Entity Framework Core

It's as like as Entity Framework, with two differences:

1- We register entity framework core by following:

```csharp
dependencyManager.RegisterEfCoreDbContext<MyAppDbContext, SqlDbContextObjectsProvider>();
```

2- MyAppDbContext has following codes:

```csharp
public MyAppDbContext()
    : base(new DbContextOptionsBuilder().UseSqlServer(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString")).Options)
{
    // This one is need for migrations/initializers etc.
}

public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbContextObjectsProvider dbContextCreationOptionsProvider)
        : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbContextCreationOptionsProvider)
{
     // This constructor has better performance, provides implicit unit of work, etc. And it is automatically used by bit framework while processing requests etc.
}
```

If you've got a complex scenario, simply drops us an [issue on github](https://github.com/bit-foundation/bit-framework/issues) or ask a question on [stackoverflow](https://stackoverflow.com/questions/tagged/bit-framework).

### Bit repository specific methods

Bit repository has several methods such as GetAll, Add, Remove etc as like as any other repository you might know, but it has following methods you can't find in other repositories:

LoadCollection - LoadReference - GetCollectionQuery

By reading the article which describes [why bit repository is optimized for N-Tier scenarios](https://docs.bit-framework.com/docs/design-backgrounds/optimized-entity-framework-for-n-tier-apps.html), you'll find out we disable lazy loading by default which improves your app performance from 3 times to 100 times based on a scenario. But how you can achieve lazy loading in case you need that? Consider following code:

```csharp
[Route("customers/customer-lazy-sample")]
public virtual void CustomerLazySample()
{
    Customer customer = DbContext.Customers.Find(1);

    // a few lines of codes...

    // Now we need customer's orders. So we simply write:

    foreach (Order order in customer.Orders /* Now entity framework loads orders of that customer. This load is not async, and does not support cancellation token )-: */)
    {

    }
}
```

Using bit repository, you can write that code as follows:

```csharp
[Route("customers/customer-lazy-sample")]
public virtual async Task CustomerLazySample(CancellationToken cancellationToken)
{
    Customer customer = await CustomersRepository.GetByIdAsync(cancellationToken, 1);

    // a few lines of codes...

    // now we need customer's orders. So we simply write:

    await CustomersRepository.LoadCollectionAsync(customer, c => c.Orders, cancellationToken); // Use async-await + cancellation token for lazy loading! (-:

    foreach (Order order in customer.Orders)
    {

    }
}
```

LoadReference is similar to LoadCollection, but you call it when you intend to load reference properties, for example, City of Customer. (Each customer has a City property called BirthLocationCity, so you can write CustomersRepository.LoadReferenceAsync(customer, c => c.BirthLocationCity, cancellationToken), and then you can access customer.BirthLocationCity property.

GetCollectionQuery returns IQueryable, for example, IQueryable of orders of that customer.