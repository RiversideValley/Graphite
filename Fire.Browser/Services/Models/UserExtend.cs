using FireBrowserWinUi3MultiCore;
using System.IO;

namespace FireBrowserWinUi3.Services.Models
{
    public class UserExtend : User
    {
        public string PicturePath { get; }
        public User FireUser { get; }

        public UserExtend(User user) : base(user)
        {
            FireUser = user;
            string path = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, user.Username, "profile_image.jpg");
            PicturePath = File.Exists(path) ? path : "ms-appx:///FireBrowserWinUi3Assets/Assets/user.png";
        }
    }
}