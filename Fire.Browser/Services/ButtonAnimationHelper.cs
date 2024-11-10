using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace FireBrowserWinUi3.Services
{
    public static class ButtonAnimationHelper
    {
        public static readonly DependencyProperty AddPointerOverAnimationProperty =
            DependencyProperty.RegisterAttached("AddPointerOverAnimation", typeof(bool), typeof(ButtonAnimationHelper), new PropertyMetadata(false, OnAddPointerOverAnimationChanged));

        public static readonly DependencyProperty AddPressedAnimationProperty =
            DependencyProperty.RegisterAttached("AddPressedAnimation", typeof(bool), typeof(ButtonAnimationHelper), new PropertyMetadata(false, OnAddPressedAnimationChanged));

        public static bool GetAddPointerOverAnimation(Button button)
        {
            return (bool)button.GetValue(AddPointerOverAnimationProperty);
        }

        public static void SetAddPointerOverAnimation(Button button, bool value)
        {
            button.SetValue(AddPointerOverAnimationProperty, value);
        }

        public static bool GetAddPressedAnimation(Button button)
        {
            return (bool)button.GetValue(AddPressedAnimationProperty);
        }

        public static void SetAddPressedAnimation(Button button, bool value)
        {
            button.SetValue(AddPressedAnimationProperty, value);
        }

        private static void OnAddPointerOverAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                bool newValue = (bool)e.NewValue;

                if (newValue)
                {
                    button.PointerEntered += Button_PointerEntered1;
                    button.PointerExited += Button_PointerExited1;
                }
                else
                {
                    button.PointerEntered -= Button_PointerEntered1;
                    button.PointerExited -= Button_PointerExited1;
                }
            }
        }

        private static void Button_PointerExited1(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Example: Revert background color on pointer exit
                var storyboard = new Storyboard();
                var colorAnimation = new ColorAnimation();
                colorAnimation.To = Microsoft.UI.Colors.Transparent; // Change this to the original color
                colorAnimation.Duration = TimeSpan.FromMilliseconds(200);
                storyboard.Children.Add(colorAnimation);
                Storyboard.SetTarget(colorAnimation, button);
                Storyboard.SetTargetProperty(colorAnimation, "(Button.Background).(SolidColorBrush.Color)");
                storyboard.Begin();
            }
        }

        private static void Button_PointerEntered1(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Example: Change background color on pointer enter
                var storyboard = new Storyboard();
                var colorAnimation = new ColorAnimation();
                colorAnimation.To = Microsoft.UI.Colors.LightGray;
                colorAnimation.Duration = TimeSpan.FromMilliseconds(200);
                storyboard.Children.Add(colorAnimation);
                Storyboard.SetTarget(colorAnimation, button);
                Storyboard.SetTargetProperty(colorAnimation, "(Button.Background).(SolidColorBrush.Color)");
                storyboard.Begin();
            }
        }

        private static void OnAddPressedAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                bool newValue = (bool)e.NewValue;

                if (newValue)
                {
                    button.PointerPressed += Button_PointerPressed;
                    button.PointerReleased += Button_PointerReleased;
                }
                else
                {
                    button.PointerPressed -= Button_PointerPressed;
                    button.PointerReleased -= Button_PointerReleased;
                }
            }
        }

        private static void Button_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Example: Revert opacity on pointer release
                var storyboard = new Storyboard();
                var opacityAnimation = new DoubleAnimation();
                opacityAnimation.To = 1.0; // Return to original opacity
                opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
                storyboard.Children.Add(opacityAnimation);
                Storyboard.SetTarget(opacityAnimation, button);
                Storyboard.SetTargetProperty(opacityAnimation, "(Button.Opacity)");
                storyboard.Begin();
            }
        }

        private static void Button_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Example: Change opacity on pointer press
                var storyboard = new Storyboard();
                var opacityAnimation = new DoubleAnimation();
                opacityAnimation.To = 0.5; // Example opacity value
                opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
                storyboard.Children.Add(opacityAnimation);
                Storyboard.SetTarget(opacityAnimation, button);
                Storyboard.SetTargetProperty(opacityAnimation, "(Button.Opacity)");
                storyboard.Begin();
            }
        }
    }
}
