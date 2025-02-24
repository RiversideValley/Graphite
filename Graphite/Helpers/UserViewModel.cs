using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.Helpers
{
	public class UserViewModel
	{
		public string Username { get; set; }
		public string Email { get; set; }
		public BitmapImage ProfileImageSource { get; set; }
	}
}
