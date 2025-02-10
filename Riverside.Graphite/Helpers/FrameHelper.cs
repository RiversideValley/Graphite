using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Helpers
{
	public static class FrameHelper
	{
		 
			public static object GetPageViewModel(this Frame frame)
				=> frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
		 
	}
}
