using Riverside.Graphite.Core;
using System.IO;

namespace Riverside.Graphite.Services.Models
{
	public class UserExtend : User
	{
		public string PicturePath { get; }
		public User FireUser { get; }

		public UserExtend(User user) : base(user)
		{
			FireUser = user;
			string path = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, user.Username, "profile_image.jpg");
			PicturePath = File.Exists(path) ? path : "ms-appx:///Riverside.Graphite.Assets/Assets/user.png";
		}
	}
}