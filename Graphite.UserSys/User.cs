using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.UserSys;

public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string WindowsUserName { get; set; }
    public bool IsFirstLaunch { get; set; }
    public string ProfileImagePath { get; set; }
    public bool HasPassword { get; set; }
    public List<string> Profiles { get; set; }
    public string ActiveProfile { get; set; }
}
