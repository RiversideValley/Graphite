
using CommunityToolkit.Mvvm.ComponentModel;
using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3Exceptions;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Desktop;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinRT.Interop;
using static System.Formats.Asn1.AsnWriter;
namespace FireBrowserWinUi3.Services
{
    // Copyright (c) Microsoft Corporation.
    // Licensed under the MIT License.
    public partial class MsalAuthService : ObservableObject, IAuthenticationService, IAuthenticationProvider
    {
        private Lazy<Task<IPublicClientApplication>> _pca;
        private string _userIdentifier = string.Empty;
        protected string ClientId { get; } = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";
        protected string[] scopes = ["User.Read"];

        public GraphServiceClient GraphClient => new GraphServiceClient(this);

        private bool _isSignedIn = false;
        public bool IsSignedIn
        {
            get => _isSignedIn;
            private set
            {
                _isSignedIn = value;
                OnPropertyChanged();
            }
        }

        public MsalAuthService()
        {
            _pca = new Lazy<Task<IPublicClientApplication>>(InitializeMsalWithCache);

        }

        /// <inheritdoc/>
        /// <remarks>
        /// Attempts to get a token silently from the cache. If this fails, the user needs to sign in.
        /// </remarks>
        public async Task<bool> IsAuthenticatedAsync()
        {
            var silentResult = await GetTokenSilentlyAsync();
            IsSignedIn = silentResult is not null;
            return IsSignedIn;
        }

        public async Task<bool> SignInAsync()
        {
            try
            {
                var result = await GetTokenSilentlyAsync();
                if (result == null)
                {
                    // If silent attempt didn't work, try an
                    // interactive sign in
                    result = await GetTokenInteractivelyAsync();
                }

                IsSignedIn = result is not null;
                return IsSignedIn;
            }
            catch (MsalClientException)
            {

                throw;
            }
            catch(MsalException)
            {
                throw;
            }
            catch (Exception ex)
            {
            
                ExceptionLogger.LogException(ex);   
            }
            // First attempt to get a token silently
            return false; 

        }

        public async Task SignOutAsync()
        {
            var pca = await _pca.Value;
// Get all accounts (there should only be one)
            // and remove them from the cache
            var accounts = await pca.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await pca.RemoveAsync(account);
            }

            // Clear the user identifier
            _userIdentifier = string.Empty;
            IsSignedIn = false;
        }

        /// <summary>
        /// Initializes a PublicClientApplication with a secure serialized cache.
        /// </summary>
        private async Task<IPublicClientApplication> InitializeMsalWithCache()
        {
            // Initialize the PublicClientApplication
            //string RedirectUri = $"msal{ClientId}://auth";
            string RedirectUri = "ms-appx-web://microsoft.aad.brokerplugin/edfc73e2-cac9-4c47-a84c-dedd3561e8b5";
            //string RedirectUri = "http://localhost";
            IntPtr mainWnd = WindowNative.GetWindowHandle(AppService.ActiveWindow);
         
            var builder = PublicClientApplicationBuilder
                                .Create(ClientId)
                                .WithSsoPolicy()
                                .WithCacheOptions(new CacheOptions() { UseSharedCache = true })
                                .WithParentActivityOrWindow(() => mainWnd)
                                .WithWindowsDesktopFeatures(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                                .WithWindowsEmbeddedBrowserSupport()
                                .WithRedirectUri(RedirectUri);

            builder = AddPlatformConfiguration(builder);

            var pca = builder.Build();

            await RegisterMsalCacheAsync(pca.UserTokenCache);

            return pca;

        } 
    

        private PublicClientApplicationBuilder AddPlatformConfiguration(PublicClientApplicationBuilder builder) =>  builder.WithIosKeychainSecurityGroup("com.microsoft.adalcache");

        private Task RegisterMsalCacheAsync(ITokenCache tokenCache) => Task.CompletedTask; 
            
        public async Task<IAccount> GetUserAccountAsync()
        {
            try
            {
                var pca = await _pca.Value;

                if (string.IsNullOrEmpty(_userIdentifier))
                {
                    // If no saved user ID, then get the first account.
                    // There should only be one account in the cache anyway.
                    var accounts = await pca.GetAccountsAsync();
                    var account = accounts.FirstOrDefault();

                    // Save the user ID so this is easier next time
                    _userIdentifier = account?.HomeAccountId.Identifier ?? string.Empty;
                    return account;
                }

                // If there's a saved user ID use it to get the account
                return await pca.GetAccountAsync(_userIdentifier);
            }
            catch (Exception)
            {
                return null;
            }
        }

            private async Task<AuthenticationResult> GetTokenSilentlyAsync()
            {
                try
                {
                    var pca = await _pca.Value;

                    var account = await GetUserAccountAsync();
                    if (account == null) return null;

                    return await pca.AcquireTokenSilent(scopes, account)
                        .ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    return null;
                }
            }

            /// <summary>
            /// Attempts to get a token interactively using the device's browser.
            /// </summary>
            private async Task<AuthenticationResult> GetTokenInteractivelyAsync()
            {
                var pca = await _pca.Value;

                var result = await pca.AcquireTokenInteractive(scopes)
                    .ExecuteAsync();

                // Store the user ID to make account retrieval easier
                _userIdentifier = result.Account.HomeAccountId.Identifier;
                return result;
            }

            public async Task AuthenticateRequestAsync(
                RequestInformation request,
                Dictionary<string, object> additionalAuthenticationContext = null,
                CancellationToken cancellationToken = default)
            {
                if (request.URI.Host == "graph.microsoft.com")
                {
                    // First try to get the token silently
                    var result = await GetTokenSilentlyAsync();
                    if (result == null)
                    {
                        // If silent acquisition fails, try interactive
                        result = await GetTokenInteractivelyAsync();
                    }

                    request.Headers.Add("Authorization", $"Bearer {result.AccessToken}");
                }
            }
      
    }
    


}
