using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace RouterControl.Infrastructure.Behaviors
{
    internal class PasswordBoxBindingBehavior : Behavior<PasswordBox>
    {
        public static readonly DependencyProperty PasswordTextProperty = DependencyProperty.Register("PasswordText",
            typeof(string), typeof(PasswordBoxBindingBehavior), new PropertyMetadata(string.Empty, PasswordTextChangedHandler));

        private bool _skipUpdate;

        protected override void OnAttached()
        {
            AssociatedObject.PasswordChanged += AssociatedObjectPasswordChanged;
            AssociatedObject.Unloaded += AssociatedObjectUnloaded;
        }

        private void AssociatedObjectPasswordChanged(object sender, RoutedEventArgs e)
        {
            _skipUpdate = true;

            PasswordText = AssociatedObject.Password;

            _skipUpdate = false;
        }

        private void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.PasswordChanged -= AssociatedObjectPasswordChanged;
            AssociatedObject.Unloaded -= AssociatedObjectUnloaded;
        }

        private void UpdatePassword(string password)
        {
            if (_skipUpdate)
                return;

            AssociatedObject.Password = password;
        }

        private static void PasswordTextChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PasswordBoxBindingBehavior)d).UpdatePassword((string)e.NewValue);
        }

        public string PasswordText
        {
            get => (string)GetValue(PasswordTextProperty);
            set => SetValue(PasswordTextProperty, value);
        }
    }
}