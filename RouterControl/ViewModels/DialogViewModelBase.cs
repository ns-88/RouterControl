using System;
using System.Threading;
using Prism.Commands;
using Prism.Services.Dialogs;

namespace RouterControl.ViewModels
{
    public class DialogViewModelBase : IDialogAware
    {
        public event Action<IDialogResult>? RequestClose;
        public string Title { get; }
        public DelegateCommand CloseDialogCommand { get; }

        public DialogViewModelBase(string title)
        {
            Title = title;
            CloseDialogCommand = new DelegateCommand(CloseDialog, CanCloseDialog);
        }

        private void CloseDialog()
        {
            RaiseRequestClose(new DialogResult());
        }

        protected void RaiseRequestClose(IDialogResult dialogResult)
        {
            Volatile.Read(ref RequestClose)?.Invoke(dialogResult);
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