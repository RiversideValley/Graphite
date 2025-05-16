using System;
using System.Collections.Generic;

namespace Riverside.Graphite.Services.Contracts;

public interface IPageService
{
	Type GetPageType(string key);
	Dictionary<string, Type> Pages { get; }
}
