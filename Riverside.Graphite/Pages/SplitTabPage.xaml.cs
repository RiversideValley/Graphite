using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Riverside.Graphite.Pages
{
	public sealed partial class SplitTabPage : Page
	{
		private Frame _activeFrame;

		public event EventHandler<Frame> ActiveFrameChanged;

		public Frame ActiveFrame
		{
			get => _activeFrame;
			private set
			{
				if (_activeFrame != value)
				{
					if (_activeFrame == LeftFrame)
					{
						VisualStateManager.GoToState(LeftFrame, "LeftFrameInactive", true);
					}
					else if (_activeFrame == RightFrame)
					{
						VisualStateManager.GoToState(RightFrame, "RightFrameInactive", true);
					}

					_activeFrame = value;

					if (_activeFrame == LeftFrame)
					{
						VisualStateManager.GoToState(LeftFrame, "LeftFrameActive", true);
					}
					else if (_activeFrame == RightFrame)
					{
						VisualStateManager.GoToState(RightFrame, "RightFrameActive", true);
					}

					ActiveFrameChanged?.Invoke(this, _activeFrame);
				}
			}
		}

		public SplitTabPage()
		{
			InitializeComponent();
			InitializeFrames();
		}

		private void InitializeFrames()
		{
			InitializeFrame(LeftFrame);
			InitializeFrame(RightFrame);

			LeftFrame.Tapped += Frame_Tapped;
			RightFrame.Tapped += Frame_Tapped;

			// Set initial active frame
			ActiveFrame = LeftFrame;
		}

		private void InitializeFrame(Frame frame)
		{
			var webContent = new WebContent();
			webContent.HorizontalAlignment = HorizontalAlignment.Stretch;
			webContent.VerticalAlignment = VerticalAlignment.Stretch;
			frame.Content = webContent;
		}

		private void Frame_Tapped(object sender, RoutedEventArgs e)
		{
			if (sender is Frame tappedFrame)
			{
				FocusFrame(tappedFrame);
			}
		}

		public void FocusFrame(Frame frame)
		{
			if (frame == LeftFrame || frame == RightFrame)
			{
				ActiveFrame = frame;
				frame.Focus(FocusState.Programmatic);
			}
		}

		public async Task<bool> NavigateActiveFrame(Uri uri)
		{
			if (ActiveFrame?.Content is WebContent webContent)
			{
			}
			return false;
		}

		public void CloseWebViews()
		{
			CloseWebViewInFrame(LeftFrame);
			CloseWebViewInFrame(RightFrame);
		}

		private void CloseWebViewInFrame(Frame frame)
		{
			if (frame.Content is WebContent webContent)
			{
			}
		}

		public void SwapFrameContents()
		{
			var leftContent = LeftFrame.Content;
			var rightContent = RightFrame.Content;

			LeftFrame.Content = rightContent;
			RightFrame.Content = leftContent;

			// Ensure the active frame visual state is maintained
			ActiveFrame = (ActiveFrame == LeftFrame) ? RightFrame : LeftFrame;
		}

		public void ResetFrames()
		{
			CloseWebViews();
			InitializeFrame(LeftFrame);
			InitializeFrame(RightFrame);
			ActiveFrame = LeftFrame;
		}
	}
}

