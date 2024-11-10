// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services.Contracts
{
    /// <summary>
    /// Service that provides authentication methods and access to 
    /// an authenticated GraphServiceClient.
    /// </summary>
    public interface IAuthenticationService : INotifyPropertyChanged
    {
        /// <summary>
        /// An authenticated GraphServiceClient for the signed-in user.
        /// </summary>
        public GraphServiceClient GraphClient { get; }

        public bool IsSignedIn { get; }

        /// <summary>
        /// Check if a user has signed in.
        /// </summary>
        /// <returns>true if a user has signed in, false if not</returns>
        public Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Attempts to sign in a user.
        /// </summary>
        /// <returns>true if authentication succeeds, false if not</returns>

        public Task<AuthenticationResult> SignInAsync();

        /// <summary>
        /// Sign out the user.
        /// </summary>
        public Task SignOutAsync();

        public Task<IAccount> GetUserAccountAsync();
    }
}
