namespace Bit.Core.Models
{
    public class LogData // Move to Bit.Server.Owin
    {
        public virtual string Key { get; set; } = default!;

        public virtual object? Value { get; set; }

        public override string ToString()
        {
            return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
        }
    }
}
