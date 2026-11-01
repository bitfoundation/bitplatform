namespace Bit.BlazorUI;

internal sealed class BitPdfBudget
{
    private long _remaining;

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
