using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.Graphite.Services.Contracts;

public interface IPageService
{
	Type GetPageType(string key);
	Dictionary<string, Type> Pages { get; }
}
