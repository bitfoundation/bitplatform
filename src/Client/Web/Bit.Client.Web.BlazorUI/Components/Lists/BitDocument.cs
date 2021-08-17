using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public class BitDocument
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string IconName { get; set; }
        public string FileType { get; set; }
        public string ModifiedBy { get; set; }
        public string DateModified { get; set; }
        public DateTimeOffset DateModifiedValue { get; set; }
        public long FileSize { get; set; }
    }
}
