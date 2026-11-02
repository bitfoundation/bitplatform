namespace Bit.BlazorUI;

internal sealed class BitPdfBudget
{
    private long _remaining;
    private static readonly AsyncLocal<RetryScope?> CurrentRetry = new();

    // Network misses restart CPU builders, not their aggregate work allowance.
    // Identities keep independent pages/fonts/catalog tasks from sharing one cap.
    internal static IDisposable BeginRetryScope() => new RetryScope();

    internal static BitPdfBudget ForRetry(long limit, object identity, string kind)
    {
        var budgets = CurrentRetry.Value?._budgets;
        if (budgets is null) return new(limit);
        var key = (identity, kind, limit);
        if (!budgets.TryGetValue(key, out var budget)) budgets[key] = budget = new(limit);
        return budget;
    }

    private sealed class RetryScope : IDisposable
    {
        internal readonly Dictionary<(object Identity, string Kind, long Limit), BitPdfBudget> _budgets;
        private readonly RetryScope? _previous;
        internal RetryScope()
        {
            _previous = CurrentRetry.Value;
            _budgets = _previous?._budgets ?? [];
            CurrentRetry.Value = this;
        }
        public void Dispose() => CurrentRetry.Value = _previous;
    }

    public BitPdfBudget(long limit)
    {
        _remaining = limit;
    }

    public bool TrySpend(long amount)
    {
        if (amount < 0 || amount > _remaining)
        {
            return false;
        }
        _remaining -= amount;
        return true;
    }
}
