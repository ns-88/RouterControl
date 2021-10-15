using Prism.Services.Dialogs;

namespace RouterControl.Views
{
    public partial class AdonisDialogHostWindow : IDialogWindow
    {
        public IDialogResult? Result { get; set; }

        public AdonisDialogHostWindow()
        {
            InitializeComponent();
        }
    }
}