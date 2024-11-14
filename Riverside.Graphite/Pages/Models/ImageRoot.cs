using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Pages.Models
{
	public class ImageRoot
	{
		public Image[] images { get; set; }
	}
	public class Image
	{
		public string url { get; set; }
		public string copyright { get; set; }
		public string copyrightlink { get; set; }
		public string title { get; set; }
	}
}
