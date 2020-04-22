using Prism.Mvvm;
using System.Runtime.CompilerServices;

namespace Bit.Core.Models
{
    public class Bindable : BindableBase
    {
        public virtual void RaiseMemberChanged([CallerMemberName] string? memberName = null)
        {
            RaisePropertyChanged(memberName);
        }

        public virtual void RaiseThisChanged()
        {
            RaisePropertyChanged(".");
        }
    }
}
