using System;

namespace Bit.Core.Models
{
    public class JobInfo
    {
        public virtual string Id { get; set; }

        public virtual string State { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
