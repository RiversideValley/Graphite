namespace Riverside.Graphite.Core.Models
{
    public class UserMigrationData
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public string WindowsUserName { get; set; }
        public bool IsFirstLaunch { get; set; }
        public string ProfileImagePath { get; set; }
        public bool HasPassword { get; set; }
    }
}
