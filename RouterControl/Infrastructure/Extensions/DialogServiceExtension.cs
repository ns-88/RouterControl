using System;
using Prism.Services.Dialogs;

namespace RouterControl.Infrastructure.Extensions
{
    using Utilities;

    internal static class DialogServiceExtension
    {
        public static void ShowDialog(this IDialogService dialogService, string name, string paramName, object paramValue, Action<IDialogResult> callback)
        {
            Guard.ThrowIfNull(dialogService, nameof(dialogService));
            Guard.ThrowIfEmptyString(paramName, nameof(paramName));
            Guard.ThrowIfNull(paramValue, nameof(paramValue));
            Guard.ThrowIfNull(callback, nameof(callback));

            dialogService.ShowDialog(name, new DialogParameters { { paramName, paramValue } }, callback);
        }

        public static DialogResult WithParameter(this DialogResult dialogResult, string name, object? value)
        {
            Guard.ThrowIfNull(dialogResult, nameof(dialogResult));
            Guard.ThrowIfEmptyString(name, nameof(name));

            dialogResult.Parameters.Add(name, value);

            return dialogResult;
        }
    }
}