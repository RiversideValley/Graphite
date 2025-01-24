// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Riverside.Graphite.Data.Core.Models;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Riverside.Graphite.Data.Core.Models.Contacts;
using Microsoft.UI.Xaml.Media;

namespace Riverside.Graphite.Data.Core.Models
{
	public class CollectionName
	{
		[Key]
		public int Id { get; set; }

		public string Name { get; set; }

		[NotMapped]
		public IEnumerable<Collection> Children { get; set; }

		[NotMapped]
		public SolidColorBrush BackgroundBrush { get; set; }	

	}

}