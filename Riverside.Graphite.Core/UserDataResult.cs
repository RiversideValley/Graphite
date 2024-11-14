using System.Collections.Generic;

namespace Riverside.Graphite.Core;

public class UserDataResult
{
	public List<User> Users { get; set; }
	public string CurrentUsername { get; set; }
}