using System;
using System.Threading;
using Prism;
using Prism.Commands;
using Prism.Services.Dialogs;

namespace RouterControl.ViewModels
{
    internal class DialogViewModelBase : ViewModelBase, IDialogAware
    {
        public event Action<IDialogResult>? RequestClose;
        public string Title { get; }
        public DelegateCommand CloseDialogCommand { get; }

        public DialogViewModelBase(string title)
        {
            Title = title;
            CloseDialogCommand = new DelegateCommand(CloseDialog, CanCloseDialog);
            IsActiveChanged += IsActiveChangedHandler;
        }

        private void IsActiveChangedHandler(object? sender, EventArgs e)
        {
            if (((IActiveAware)this).IsActive)
                OnViewOpened();
            else
                OnViewClosed();
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult());
        }

        protected void RaiseRequestClose(IDialogResult dialogResult)
        {
            Volatile.Read(ref RequestClose)?.Invoke(dialogResult);
        }

        protected virtual void OnViewOpened()
        {

        }

        protected virtual void OnViewClosed()
        {

        }

        public virtual bool CanCloseDialog()
        {
            return true;
        }

        public virtual void OnDialogClosed()
        {

        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {

        }
    }
}