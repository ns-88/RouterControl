using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Prism;

namespace RouterControl.Infrastructure.Behaviors
{
    internal class WindowActiveChangedBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            
            AssociatedObject.Loaded += AssociatedObjectLoaded;
            AssociatedObject.Closed += AssociatedObjectClosed;
        }

        private static void SetIsActiveState(ContentControl control, bool state)
        {
            if (control.Content is not UserControl { DataContext: IActiveAware dataContext })
                return;

            dataContext.IsActive = state;
        }

        private void AssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            SetIsActiveState(AssociatedObject, true);
        }

        private void AssociatedObjectClosed(object? sender, EventArgs e)
        {
            SetIsActiveState(AssociatedObject, false);

            AssociatedObject.Loaded -= AssociatedObjectLoaded;
            AssociatedObject.Closed -= AssociatedObjectClosed;
        }
    }
}