using System;
using System.Collections.Generic;
using System.Threading;
using Prism;
using Prism.Mvvm;

namespace RouterControl.ViewModels
{
    internal class ViewModelBase : BindableBase, IActiveAware
    {
        public event EventHandler? IsActiveChanged;

        #region IsActive
        private bool _isActive;
        bool IActiveAware.IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                RaiseIsActiveChanged();
            }
        }
        #endregion

        private void RaiseIsActiveChanged()
        {
            Volatile.Read(ref IsActiveChanged)?.Invoke(this, EventArgs.Empty);
        }

        protected virtual IEnumerable<string> GetPropertyNames()
        {
            yield break;
        }

        protected void RaisePropertiesChanged()
        {
            foreach (var propertyName in GetPropertyNames())
            {
                RaisePropertyChanged(propertyName);
            }
        }
    }
}