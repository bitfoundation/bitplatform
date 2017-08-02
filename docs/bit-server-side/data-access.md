# Bit Data Access

You can use Bit Data Access components, or you can use your own preferred way to read/manipulate database. But why you might choose Bit data access anyway? These are answers:

1- True async support. Application with async code gets scaled better, but what does this mean to us? Each server side app has limited threads. Threads are workers and your app works because they work. Threads count is limited, you've not unlimited threads, so you've to use them carefully. When a request comes to your web api action, and you get data from database using entity framework, your thread (your valuable thread) waits until database returns data. But that wait is useless. In case you use async-await, your thread executes other requests instead of waiting for a database.

2- True cancellation token support. There is CancellationToken in every web api action you develop. If user/operator closes its browser, or if you cancel request at client side programmatically, that cancellation token gets notified. Almost all bit framework's methods accept cancellation token, and they stop their work if cancellation token gets notified.

3- Bit Data Access components are optimized for N-Tier app development. To have a better understanding what does this mean see [here](https://docs.bit-framework.com/docs/design-backgrounds/optimized-entity-framework-for-n-tier-apps.html).

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
        // This one is need for migrations/initializers etc.
    }

    public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbConnectionProvider dbConnectionProvider)
        : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbConnectionProvider)
    {
        // This one has better performance, provides implicit unit of work, etc
    }

    public virtual DbSet<Customer> Customers { get; set; }
}
```

App environment provider provides you a way to access your configuration. By default, it reads configs from environments.json file. You don't have to use that in this case, your db context just needs a connection string, read it from app environment provider, asp.net core's configuration provider, app/web config files or write connection string there directly.

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

1- We register entity framework by following:

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
    // This one has better performance, provides implicit unit of work, etc
}
```

If you've got a complex scenario, simply drops us an [issue on github](https://github.com/bit-foundation/bit-framework/issues) or ask a question on [stackoverflow](https://stackoverflow.com/questions/tagged/bit-framework).