using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.Model
{
    public interface ITrackable
    {
        void BeginTrack();

        void AcceptChanges();

        void RevertChanges();

        bool HasChanges { get; }
    }

    public class Bindable : BindableBase, ITrackable
    {
        public virtual void RaiseMemberChanged([CallerMemberName] string memberName = null)
        {
            RaisePropertyChanged(memberName);
        }

        public virtual void RaiseThisChanged()
        {
            RaisePropertyChanged(".");
        }

        private List<(string propName, object originalPropValue)> changedProps = null;

        private bool IsBeingTracked => changedProps != null;

        public virtual void OnPropertyChanged(string propertyName, object before, object after)
        {
            if (IsBeingTracked)
            {
                if (!changedProps.Any(p => p.propName == propertyName))
                    changedProps.Add((propertyName, before));
            }
        }

        void ITrackable.BeginTrack()
        {
            if (IsBeingTracked)
                throw new InvalidOperationException("Object is being tracked");

            changedProps = new List<(string propName, object originalPropValue)>();
        }

        void ITrackable.AcceptChanges()
        {
            if (!IsBeingTracked)
                throw new InvalidOperationException("Object isn't being tracked");

            changedProps.Clear();
        }

        void ITrackable.RevertChanges()
        {
            if (!IsBeingTracked)
                throw new InvalidOperationException("Object isn't being tracked");

            List<(string propName, object originalPropValue)> localProps = changedProps;

            try
            {
                changedProps = null; // To prevent calling OnPropertyChanged with IsBeingTracked value true.
                foreach ((string propName, object originalPropValue) in localProps)
                {
                    PropertyInfo propInfo = GetType().GetProperty(propName);

                    if (propInfo == null)
                        throw new InvalidOperationException($"Property {propName} could not be found");

                    if (propInfo.CanWrite)
                        propInfo.SetValue(this, originalPropValue);
                }
            }
            finally
            {
                changedProps = new List<(string propName, object originalPropValue)>();
            }
        }

        bool ITrackable.HasChanges => IsBeingTracked && changedProps.Any();
    }
}
