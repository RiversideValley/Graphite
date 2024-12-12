using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;

namespace Riverside.Graphite.Pages
{
	public sealed partial class FlappyBirdPage : Page
	{
		private const int Gravity = 2;
		private const int JumpStrength = 10;
		private int birdVelocity = 0;
		private int score = 0;
		private Random random = new Random();
		private DispatcherTimer gameTimer;

		public FlappyBirdPage()
		{
			this.InitializeComponent();
			this.Loaded += FlappyBirdPage_Loaded;
			InitializeGame();
		}

		private void FlappyBirdPage_Loaded(object sender, RoutedEventArgs e)
		{
			// Set focus to the page to enable key events
			this.Focus(FocusState.Programmatic);
		}

		private void InitializeGame()
		{
			gameTimer = new DispatcherTimer();
			gameTimer.Tick += GameTimer_Tick;
			gameTimer.Interval = TimeSpan.FromMilliseconds(16); // Approximately 60 FPS
		}

		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			StartGame();
		}

		private void StartGame()
		{
			score = 0;
			ScoreText.Text = "Score: 0";
			ResetBirdPosition();
			ResetPipePosition();
			gameTimer.Start();
			StartButton.Visibility = Visibility.Collapsed;

			// Ensure the page has focus to capture key presses
			this.Focus(FocusState.Programmatic);
		}

		private void GameTimer_Tick(object sender, object e)
		{
			UpdateBirdPosition();
			UpdatePipePosition();
			CheckCollision();
			UpdateScore();
		}

		protected override void OnKeyDown(KeyRoutedEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.Key == Windows.System.VirtualKey.Space)
			{
				birdVelocity = JumpStrength;
				e.Handled = true;
			}
		}

		private void UpdateBirdPosition()
		{
			birdVelocity += Gravity;
			double newPosition = Canvas.GetTop(Bird) + birdVelocity;
			newPosition = Math.Max(0, Math.Min(newPosition, GameCanvas.ActualHeight - Bird.ActualHeight));
			Canvas.SetTop(Bird, newPosition);
		}

		private void UpdatePipePosition()
		{
			Canvas.SetLeft(TopPipe, Canvas.GetLeft(TopPipe) - 2);
			Canvas.SetLeft(BottomPipe, Canvas.GetLeft(BottomPipe) - 2);

			if (Canvas.GetLeft(TopPipe) < -60)
			{
				ResetPipePosition();
			}
		}

		private void ResetPipePosition()
		{
			double gapPosition = random.Next(100, (int)GameCanvas.ActualHeight - 300);
			Canvas.SetLeft(TopPipe, GameCanvas.ActualWidth);
			Canvas.SetTop(TopPipe, gapPosition - 200);
			Canvas.SetLeft(BottomPipe, GameCanvas.ActualWidth);
			Canvas.SetTop(BottomPipe, gapPosition + 100);
		}

		private void ResetBirdPosition()
		{
			Canvas.SetTop(Bird, GameCanvas.ActualHeight / 2);
			birdVelocity = 0;
		}

		private void CheckCollision()
		{
			Rect birdRect = new Rect(Canvas.GetLeft(Bird), Canvas.GetTop(Bird), Bird.ActualWidth, Bird.ActualHeight);
			Rect topPipeRect = new Rect(Canvas.GetLeft(TopPipe), Canvas.GetTop(TopPipe), TopPipe.ActualWidth, TopPipe.ActualHeight);
			Rect bottomPipeRect = new Rect(Canvas.GetLeft(BottomPipe), Canvas.GetTop(BottomPipe), BottomPipe.ActualWidth, BottomPipe.ActualHeight);

			if (birdRect.IntersectsWith(topPipeRect) || birdRect.IntersectsWith(bottomPipeRect) ||
				Canvas.GetTop(Bird) <= 0 || Canvas.GetTop(Bird) >= GameCanvas.ActualHeight - Bird.ActualHeight)
			{
				GameOver();
			}
		}

		private void UpdateScore()
		{
			if (Canvas.GetLeft(TopPipe) == 98) // Bird just passed the pipe
			{
				score++;
				ScoreText.Text = $"Score: {score}";
			}
		}

		private void GameOver()
		{
			gameTimer.Stop();
			StartButton.Visibility = Visibility.Visible;
		}
	}
}

