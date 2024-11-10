using Microsoft.Identity.Client;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services
{
    public static class AuthenticationServcie
    {
        public static Task RegisterMsalCacheAsync(ITokenCache tokenCache)
        {
            // MSAL registers it's own cache on iOS
            return Task.CompletedTask;
        }

        public static PublicClientApplicationBuilder AddPlatformConfiguration(PublicClientApplicationBuilder builder)
        {
            // Configure keychain access group
            return builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");
        }
    }
}
