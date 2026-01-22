using Medallion.Threading;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// Creates distributed lock by given lock key.
/// It can be any implementation of IDistributedLock such as Redis, SqlServer, PostgreSQL, etc.
/// </summary>
public delegate IDistributedLock DistributedLockFactory(string lockKey);
