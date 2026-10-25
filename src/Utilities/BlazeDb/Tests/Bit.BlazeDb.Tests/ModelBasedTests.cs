using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Randomized differential testing: long random sequences of writes, transactions, savepoints, bulk
/// writes, checkpoints and reopens run against the engine and against a plain dictionary that
/// models what the engine should hold. After every step the table, every index and the persisted
/// copy are compared with the model, so a disagreement names the seed and step that produced it.
/// Values come from small domains on purpose, so keys and unique values collide often.
/// </summary>
[TestClass]
public class ModelBasedTests
{
    private sealed record Values(int Id, string Username, int TenantId, string Email, int? Slot, DateTime CreatedAt)
    {
        public static Values Of(Account a) => new(a.Id, a.Username, a.TenantId, a.Email, a.Slot, a.CreatedAt);

        public Account ToAccount() => new()
        {
            Id = Id, Username = Username, TenantId = TenantId, Email = Email, Slot = Slot, CreatedAt = CreatedAt,
        };
    }

    private const int Steps = 600;

    public static IEnumerable<object[]> Seeds => Enumerable.Range(1, 12).Select(seed => new object[] { seed });

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public async Task Engine_Matches_The_Model(int seed)
    {
        var run = new Run(seed);
        try
        {
            await run.ExecuteAsync();
        }
        finally
        {
            await run.DisposeAsync();
        }
    }

    private sealed class Run(int seed) : IAsyncDisposable
    {
        private static readonly string[] Usernames = ["ada", "grace", "alan", "edsger", "barbara", "ken", "", "linus"];
        private static readonly string[] Emails = ["a@x", "b@x", "c@x", "A@x", "d@x", ""];
        private static readonly DateTime Epoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly Random _random = new(seed);
        private readonly BlazeDbInMemoryStorage _storage = new();
        private BlazeDbDatabase _db = null!;
        private BlazeDbTable<int, Account> _accounts = null!;
        private Dictionary<int, Values> _model = [];

        // Model copies taken when a transaction or savepoint began, innermost last.
        private Dictionary<int, Values>? _transactionStart;
        private BlazeDbTransaction? _transaction;
        private readonly List<(BlazeDbSavepoint Savepoint, Dictionary<int, Values> Model)> _savepoints = [];
        private int _step;

        // Rebuilt purely from change notifications. It must always equal the committed state: a
        // notification that reported an uncommitted, rolled-back or intermediate state - or missed a
        // committed one - shows up as a difference.
        private readonly Dictionary<int, Values> _shadow = [];

        private BlazeDbDatabaseOptions Options(IBlazeDbStorage storage) => new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
            CheckpointWalSize = 2048,
        }.AddTable(Account.Table);

        public async Task ExecuteAsync()
        {
            _db = await BlazeDbDatabase.OpenAsync(Options(_storage));
            _accounts = _db.GetTable(Account.Table);
            _accounts.Changed += (_, e) =>
            {
                foreach (var change in e.Changes)
                {
                    var known = _shadow.TryGetValue(change.Key, out var before);
                    Assert.AreEqual(change.Kind != BlazeDbChangeKind.Inserted, known, Context($"notified {change.Kind} of {change.Key}"));
                    if (known)
                    {
                        Assert.AreEqual(before, Values.Of(change.OldRow!), Context($"old row of {change.Key}"));
                    }
                    if (change.Kind == BlazeDbChangeKind.Deleted)
                    {
                        _shadow.Remove(change.Key);
                    }
                    else
                    {
                        _shadow[change.Key] = Values.Of(change.NewRow!);
                    }
                }
            };
            for (_step = 0; _step < Steps; _step++)
            {
                await StepAsync();
                Verify();
            }
            if (_transaction is not null)
            {
                Commit();
            }
            await _db.FlushAsync();
            await VerifyPersistedAsync();
        }

        private async Task StepAsync()
        {
            switch (_random.Next(20))
            {
                case 0 or 1 or 2:
                    Write("Insert", RandomRow(), (row, model) => Insert(row, model), row => _accounts.Insert(row));
                    break;
                case 3 or 4:
                    Write("Update", RandomRow(existingBias: true), (row, model) => Update(row, model), row => _accounts.Update(row));
                    break;
                case 5 or 6:
                    Write("Upsert", RandomRow(existingBias: true), (row, model) =>
                    {
                        if (model.ContainsKey(row.Id))
                        {
                            Update(row, model);
                        }
                        else
                        {
                            Insert(row, model);
                        }
                    }, row => _accounts.Upsert(row));
                    break;
                case 7:
                {
                    var id = RandomId();
                    Assert.AreEqual(_model.Remove(id), _accounts.Delete(id), Context("Delete"));
                    break;
                }
                case 8:
                {
                    // Optimistic update: the comparison is the row the table holds, or a stale copy of it.
                    var row = RandomRow(existingBias: true);
                    var held = _accounts.Get(row.Id);
                    var comparison = held is not null && _random.Next(2) == 0 ? held : Values.Of(row).ToAccount();
                    var expectSuccess = held is not null && ReferenceEquals(comparison, held) && CanWrite(row, _model, replacing: true);
                    var conflicts = held is not null && ReferenceEquals(comparison, held) && !CanWrite(row, _model, replacing: true);
                    if (conflicts)
                    {
                        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => _accounts.TryUpdate(row, comparison), Context("TryUpdate"));
                    }
                    else
                    {
                        Assert.AreEqual(expectSuccess, _accounts.TryUpdate(row, comparison), Context("TryUpdate"));
                        if (expectSuccess)
                        {
                            _model[row.Id] = Values.Of(row);
                        }
                    }
                    break;
                }
                case 9:
                    InsertMany();
                    break;
                case 10:
                    RunUnit();
                    break;
                case 11 or 12:
                    TransactionControl();
                    break;
                case 13:
                {
                    var tenant = _random.Next(4);
                    var expected = _model.Values.Count(v => v.TenantId == tenant);
                    var deleted = _accounts.DeleteWhere(a => a.TenantId == tenant && a.Id % 3 == 0);
                    var removed = _model.Values.Where(v => v.TenantId == tenant && v.Id % 3 == 0).Select(v => v.Id).ToList();
                    removed.ForEach(id => _model.Remove(id));
                    Assert.AreEqual(removed.Count, deleted, Context("DeleteWhere"));
                    Assert.IsTrue(expected >= deleted);
                    break;
                }
                case 14:
                    if (_transaction is null)
                    {
                        await _db.CheckpointAsync();
                    }
                    break;
                case 15:
                    if (_transaction is null)
                    {
                        await _db.FlushAsync();
                        await VerifyPersistedAsync();
                    }
                    break;
                default:
                    Write("Insert", RandomRow(), (row, model) => Insert(row, model), row => _accounts.Insert(row));
                    break;
            }
        }

        private void TransactionControl()
        {
            if (_transaction is null)
            {
                _transaction = _db.BeginTransaction();
                _transactionStart = Copy(_model);
                return;
            }
            switch (_random.Next(6))
            {
                case 0:
                    Commit();
                    break;
                case 1:
                    _transaction.Rollback();
                    _model = _transactionStart!;
                    EndTransaction();
                    break;
                case 2 or 3:
                    _savepoints.Add((_transaction.CreateSavepoint(), Copy(_model)));
                    break;
                default:
                    if (_savepoints.Count > 0)
                    {
                        var index = _random.Next(_savepoints.Count);
                        _transaction.RollbackTo(_savepoints[index].Savepoint);
                        _model = Copy(_savepoints[index].Model);
                        _savepoints.RemoveRange(index + 1, _savepoints.Count - index - 1);
                    }
                    break;
            }
        }

        private void Commit()
        {
            _transaction!.Commit();
            EndTransaction();
        }

        private void EndTransaction()
        {
            _transaction = null;
            _transactionStart = null;
            _savepoints.Clear();
        }

        private void InsertMany()
        {
            var rows = Enumerable.Range(0, _random.Next(1, 5)).Select(_ => RandomRow()).ToList();
            var trial = Copy(_model);
            Exception? expected = null;
            foreach (var row in rows)
            {
                expected = Try(() => Insert(row, trial));
                if (expected is not null)
                {
                    break;
                }
            }
            var actual = Try(() => _accounts.InsertMany(rows));
            AssertSameOutcome(expected, actual, "InsertMany");
            if (expected is null)
            {
                _model = trial;
            }
        }

        private void RunUnit()
        {
            var rows = Enumerable.Range(0, _random.Next(1, 4)).Select(_ => RandomRow(existingBias: true)).ToList();
            var fail = _random.Next(3) == 0;
            var trial = Copy(_model);
            Exception? expected = null;
            foreach (var row in rows)
            {
                expected = Try(() =>
                {
                    if (trial.ContainsKey(row.Id))
                    {
                        Update(row, trial);
                    }
                    else
                    {
                        Insert(row, trial);
                    }
                });
                if (expected is not null)
                {
                    break;
                }
            }
            expected ??= fail ? new InvalidOperationException() : null;

            var actual = Try(() => _db.RunInTransaction(() =>
            {
                foreach (var row in rows)
                {
                    _accounts.Upsert(row);
                }
                if (fail)
                {
                    throw new InvalidOperationException("planned failure");
                }
            }));
            AssertSameOutcome(expected, actual, "RunInTransaction");
            if (expected is null)
            {
                _model = trial;
            }
        }

        private void Write(string operation, Account row, Action<Account, Dictionary<int, Values>> model, Action<Account> engine)
        {
            var expected = Try(() => model(row, _model));
            var actual = Try(() => engine(row));
            AssertSameOutcome(expected, actual, operation);
        }

        private void AssertSameOutcome(Exception? expected, Exception? actual, string operation) =>
            Assert.AreEqual(expected?.GetType(), actual?.GetType(), Context(operation) + $" ({actual?.Message})");

        private static Exception? Try(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        // ---- The model's rules ----

        private static void Insert(Account row, Dictionary<int, Values> model)
        {
            if (model.ContainsKey(row.Id))
            {
                throw new BlazeDbDuplicateKeyException("accounts", row.Id);
            }
            if (!CanWrite(row, model, replacing: false))
            {
                throw new BlazeDbUniqueConstraintViolationException("accounts", "?", "?");
            }
            model[row.Id] = Values.Of(row);
        }

        private static void Update(Account row, Dictionary<int, Values> model)
        {
            if (!model.ContainsKey(row.Id))
            {
                throw new KeyNotFoundException();
            }
            if (!CanWrite(row, model, replacing: true))
            {
                throw new BlazeDbUniqueConstraintViolationException("accounts", "?", "?");
            }
            model[row.Id] = Values.Of(row);
        }

        private static bool CanWrite(Account row, Dictionary<int, Values> model, bool replacing)
        {
            foreach (var other in model.Values)
            {
                if (replacing && other.Id == row.Id)
                {
                    continue;
                }
                if (other.Username == row.Username ||
                    (row.Slot is not null && other.Slot == row.Slot) ||
                    (other.TenantId == row.TenantId && other.Email == row.Email))
                {
                    return false;
                }
            }
            return true;
        }

        // ---- Verification ----

        private void Verify()
        {
            Assert.AreEqual(_model.Count, _accounts.Count, Context("Count"));
            foreach (var (id, values) in _model)
            {
                var row = _accounts.Get(id);
                Assert.IsNotNull(row, Context($"Get({id})"));
                Assert.AreEqual(values, Values.Of(row), Context($"Get({id})"));
            }

            for (var tenant = 0; tenant < 4; tenant++)
            {
                AssertSameIds(_model.Values.Where(v => v.TenantId == tenant), _accounts.Lookup(Account.Indexes.TenantId, tenant), $"TenantId={tenant}");
            }
            foreach (var username in Usernames)
            {
                AssertSameIds(_model.Values.Where(v => v.Username == username), _accounts.Lookup(Account.Indexes.Username, username), $"Username={username}");
            }
            var tenantEmail = (_random.Next(4), Emails[_random.Next(Emails.Length)]);
            AssertSameIds(
                _model.Values.Where(v => (v.TenantId, v.Email) == tenantEmail),
                _accounts.Lookup(Account.Indexes.TenantEmail, tenantEmail), $"TenantEmail={tenantEmail}");

            var byEmail = _accounts.OrderBy(Account.Indexes.Email).Select(a => a.Email).ToList();
            CollectionAssert.AreEqual(_model.Values.Select(v => v.Email).Order(StringComparer.Ordinal).ToList(), byEmail, Context("OrderBy(Email)"));

            var (from, to) = (_random.Next(-1, 8), _random.Next(-1, 8));
            var (fromExclusive, toExclusive) = (_random.Next(2) == 0, _random.Next(2) == 0);
            AssertSameIds(
                _model.Values.Where(v => (fromExclusive ? v.Slot > from : v.Slot >= from) && (toExclusive ? v.Slot < to : v.Slot <= to)),
                _accounts.Range(
                    Account.Indexes.Slot,
                    fromExclusive ? BlazeDbBound<int?>.Exclusive(from) : from,
                    toExclusive ? BlazeDbBound<int?>.Exclusive(to) : to),
                $"Slot in {(fromExclusive ? '(' : '[')}{from}, {to}{(toExclusive ? ')' : ']')}");
            var slotsDescending = _accounts.OrderBy(Account.Indexes.Slot, descending: true).Select(a => a.Slot).ToList();
            CollectionAssert.AreEqual(_model.Values.Select(v => v.Slot).OrderDescending().ToList(), slotsDescending, Context("OrderBy(Slot desc)"));

            var tenantFrom = _random.Next(4);
            var created = _accounts.Range(Account.Indexes.TenantCreated, (tenantFrom, DateTime.MinValue), (tenantFrom, DateTime.MaxValue)).ToList();
            AssertSameIds(_model.Values.Where(v => v.TenantId == tenantFrom), created, $"TenantCreated tenant {tenantFrom}");
            CollectionAssert.AreEqual(created.Select(a => a.CreatedAt).Order().ToList(), created.Select(a => a.CreatedAt).ToList(), Context("TenantCreated order"));

            var issues = _db.VerifyIntegrity();
            Assert.AreEqual(0, issues.Count, Context("VerifyIntegrity") + string.Join("; ", issues));

            CollectionAssert.AreEquivalent((_transactionStart ?? _model).ToList(), _shadow.ToList(), Context("notifications"));
        }

        /// <summary>Reopens a copy of storage and checks it holds exactly the committed rows.</summary>
        private async Task VerifyPersistedAsync()
        {
            var committed = _transactionStart ?? _model;
            await using var reopened = await BlazeDbDatabase.OpenAsync(Options(_storage.Clone()));
            var recovered = reopened.GetTable(Account.Table);
            Assert.AreEqual(committed.Count, recovered.Count, Context("Reopen count"));
            foreach (var (id, values) in committed)
            {
                Assert.AreEqual(values, Values.Of(recovered.Get(id) ?? throw new AssertFailedException(Context($"Reopen missing {id}"))), Context($"Reopen {id}"));
            }
            Assert.AreEqual(0, reopened.VerifyIntegrity().Count, Context("Reopen integrity"));
        }

        private void AssertSameIds(IEnumerable<Values> expected, IEnumerable<Account> actual, string what) =>
            CollectionAssert.AreEquivalent(expected.Select(v => v.Id).ToList(), actual.Select(a => a.Id).ToList(), Context(what));

        private string Context(string what) => $"seed {seed}, step {_step}: {what}";

        // ---- Random data ----

        private int RandomId() => _random.Next(30);

        private Account RandomRow(bool existingBias = false)
        {
            var id = existingBias && _model.Count > 0 && _random.Next(3) > 0
                ? _model.Keys.ElementAt(_random.Next(_model.Count))
                : RandomId();
            return new Account
            {
                Id = id,
                Username = Usernames[_random.Next(Usernames.Length)] + (_random.Next(3) == 0 ? "" : id.ToString()),
                TenantId = _random.Next(4),
                Email = Emails[_random.Next(Emails.Length)],
                Slot = _random.Next(3) == 0 ? null : _random.Next(8),
                CreatedAt = Epoch.AddMinutes(_random.Next(50)),
            };
        }

        private static Dictionary<int, Values> Copy(Dictionary<int, Values> model) => new(model);

        public async ValueTask DisposeAsync()
        {
            if (_db is not null)
            {
                await _db.DisposeAsync();
            }
        }
    }
}
