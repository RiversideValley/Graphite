using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using System.Numerics;
using WinRT.Interop;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Graphite.Setup.OOBE
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetupWelcome : Window
    {
        private readonly Random random = new Random();
        private readonly List<Ellipse> particles = new List<Ellipse>();
        private readonly DispatcherTimer particleTimer;
        private Compositor _compositor;
        private AppWindow _appWindow;

        public SetupWelcome()
        {
            this.InitializeComponent();

            _compositor = ElementCompositionPreview.GetElementVisual(Content).Compositor;

            // Get the AppWindow for the current window
            var windowHandle = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonHoverForegroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonForegroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Transparent;
            // Set the icon
            _appWindow.SetIcon("Assets/GraphiteIcon.ico");

            // Initialize particle system
            particleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            particleTimer.Tick += ParticleTimer_Tick;

            // Create initial particles
            CreateParticles();
            particleTimer.Start();

            // Start the welcome animation sequence
            StartWelcomeAnimation();
        }

        private void CreateParticles()
        {
            for (int i = 0; i < 15; i++)
            {
                var particle = new Ellipse
                {
                    Width = random.Next(4, 8),
                    Height = random.Next(4, 8),
                    Fill = new SolidColorBrush(Colors.White),
                    Opacity = 0.3
                };

                Canvas.SetLeft(particle, random.Next(0, (int)Bounds.Width));
                Canvas.SetTop(particle, random.Next(0, (int)Bounds.Height));

                particles.Add(particle);
                ParticleCanvas.Children.Add(particle);
            }
        }

        private void ParticleTimer_Tick(object sender, object e)
        {
            foreach (var particle in particles)
            {
                var top = Canvas.GetTop(particle);
                var left = Canvas.GetLeft(particle);

                // Move particle
                top -= random.Next(1, 3);
                left += (random.NextDouble() - 0.5) * 2;

                // Reset particle if it goes off screen
                if (top < -10)
                {
                    top = Bounds.Height + 10;
                    left = random.Next(0, (int)Bounds.Width);
                }

                Canvas.SetTop(particle, top);
                Canvas.SetLeft(particle, left);

                // Animate opacity
                particle.Opacity = 0.3 + (Math.Sin(top / 50) * 0.2);
            }
        }

        private void StartWelcomeAnimation()
        {
            // Animate logo
            var logoVisual = ElementCompositionPreview.GetElementVisual(LogoImage);
            logoVisual.Scale = new Vector3(0.8f, 0.8f, 1);

            var animation = _compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(0.0f, new Vector3(0.8f, 0.8f, 1));
            animation.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1));
            animation.Duration = TimeSpan.FromSeconds(1);

            // Add spring effect
            var spring = _compositor.CreateSpringVector3Animation();
            spring.InitialVelocity = new Vector3(0, 0, 0);
            spring.DampingRatio = 0.6f;
            spring.Period = TimeSpan.FromSeconds(0.5);
            spring.FinalValue = new Vector3(1, 1, 1);

            logoVisual.StartAnimation("Scale", animation);
            logoVisual.StartAnimation("Scale", spring);

            // Simulate loading
            DispatcherQueue.TryEnqueue(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000);
                LoadingRing.IsActive = false;

                // Show and animate the buttons
                NextSetupButton.Visibility = Visibility.Visible;
                RestoreBackupButton.Visibility = Visibility.Visible;
                AnimateButtonAppearance(NextSetupButton);
                AnimateButtonAppearance(RestoreBackupButton);
            });
        }

        private void AnimateButtonAppearance(Button button)
        {
            var buttonVisual = ElementCompositionPreview.GetElementVisual(button);
            buttonVisual.Opacity = 0f;
            buttonVisual.Offset = new Vector3(0, 50, 0);

            var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0f, 0f);
            opacityAnimation.InsertKeyFrame(1f, 1f);
            opacityAnimation.Duration = TimeSpan.FromSeconds(0.5);

            var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(0f, new Vector3(0, 50, 0));
            offsetAnimation.InsertKeyFrame(1f, new Vector3(0, 0, 0));
            offsetAnimation.Duration = TimeSpan.FromSeconds(0.8);

            buttonVisual.StartAnimation("Opacity", opacityAnimation);
            buttonVisual.StartAnimation("Offset", offsetAnimation);
        }

        private void NextSetupButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = new Frame();
            this.Content = rootFrame;
            particleTimer.Stop();
            rootFrame.Navigate(typeof(OOBEUser));
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            //var backupRestoreWindow = new BackupRestoreWindow();

            //backupRestoreWindow.Activate();
        }
    }
}
