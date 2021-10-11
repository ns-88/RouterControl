using Prism.Services.Dialogs;
using RouterControl.Infrastructure.Utilities;

namespace RouterControl.Infrastructure.Extensions
{
    internal static class DialogServiceExtension
    {
        public static void ShowDialog(this IDialogService dialogService, string name, string paramName, object paramValue)
        {
            Guard.ThrowIfNull(dialogService, nameof(dialogService));
            Guard.ThrowIfEmptyString(paramName, nameof(paramName));
            Guard.ThrowIfNull(paramValue, nameof(paramValue));

            dialogService.ShowDialog(name, new DialogParameters { { paramName, paramValue } }, null);
        }
    }
}