using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Pages.Models;

public record TrendingListItem(string webSearchUrl, string name, string url, string text);
