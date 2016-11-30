using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Model.Contracts
{
    public interface IWithDefaultKey<TKey>
    {
        TKey Id { get; set; }
    }
}
