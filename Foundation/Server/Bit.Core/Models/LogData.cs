namespace Foundation.Core.Models
{
    public class LogData
    {
        public virtual string Key { get; set; }

        public virtual object Value { get; set; }

        public override string ToString()
        {
            return Key == null ? base.ToString() : Key;
        }
    }
}
