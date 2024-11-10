using System.Collections.Generic;

namespace Fire.Browser.Core;

public class UserDataResult
{
    public List<User> Users { get; set; }
    public string CurrentUsername { get; set; }
}