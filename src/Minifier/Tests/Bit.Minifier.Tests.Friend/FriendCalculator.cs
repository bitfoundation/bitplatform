using Bit.Minifier.Tests.Library;

namespace Bit.Minifier.Tests.Friend;

public class FriendCalculator : Calculator
{
    public int SquareThroughInternals(int value)
    {
        var result = InternalSquare(value);
        return result + InternalCounter;
    }

    public int HalfThroughInternals(int value) => Rounding.Half(value);

    public async Task<int> AddTwiceAsync(int value)
    {
        await AddAsync(value);
        return await AddAsync(value);
    }

    protected override int Shift(int value) => value + 1;
}
