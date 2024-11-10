using FireBrowserWinUi3MultiCore;
using System.IO;
using System.Threading.Tasks;

namespace FireBrowserWinUi3Core.Models;

public class Data
{
    public static string TotpFilePath { get; private set; }

    public static async Task Init()
    {
        string currentUsername = AuthService.CurrentUser.Username;
        string username = currentUsername;
        string userFolderPath = Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, username, "Database");
        TotpFilePath = Path.Combine(userFolderPath, "2FA.json");
    }
}