using System;

namespace Bit.Core.Models
{
    public class JobInfo
    {
        public virtual string Id { get; set; } = default!;

        public virtual string State { get; set; } = default!;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
