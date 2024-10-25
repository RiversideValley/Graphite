using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3.Services.Models;
using FireBrowserWinUi3Core.Helpers;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.UI.Dispatching;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using WinRT.Interop;



namespace FireBrowserWinUi3.Services
{
    internal class AzBackupService
    {
        private string AzureStorageConnectionString { get; set; }

        protected private void SET_AZConnectionsString(string connString)
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(AzureStorageConnectionString)] = AzureStorageConnectionString = connString;
        }

        protected internal string ConnString { get; } = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(AzureStorageConnectionString)] as string;
        protected internal string StoragAccountName { get; set; }
        protected internal string ContainerName { get; set; }

        protected internal string TableName { get; set; } = "TrackerBackups";
        protected internal object UserWindows { get; set; }
        
        protected FireBrowserWinUi3MultiCore.User FireUser { get; set; }


        private AzBackupService(FireBrowserWinUi3MultiCore.User fireUser, string _storageName, string _containerName)
        {

            StoragAccountName = _storageName;
            ContainerName = _containerName ?? string.Empty;
            FireUser = fireUser;

        }
        public AzBackupService(string connString, string storagAccountName, string containerName, FireBrowserWinUi3MultiCore.User user) : this(user, storagAccountName, containerName)
        {
            SET_AZConnectionsString(connString);
        }

        #region WindowsGetUserBetaClasses

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        public const uint TOKEN_READ = 0x20008;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool LookupAccountSid(
        string lpSystemName,
        byte[] Sid,
        out string lpName,
        out int cchName,
        out string lpRefDomainName,
        int cchRefDomainName,
        out int peUse);

        private Task<WindowsIdentity> DisplayCurrentUserInformation()
        {
            try
            {
                WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(windowsIdentity);

                string userName = principal.Identity.Name;
                string userSID = windowsIdentity.User.Value;
                string userDomainName = windowsIdentity.Name.Split('\\')[0];

                Console.WriteLine($"User: {userName}");
                Console.WriteLine($"SID: {userSID}");
                Console.WriteLine($"Domain: {userDomainName}");
                return Task.FromResult(windowsIdentity);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user information: {ex.Message}");
                Task.FromException(ex);
            }

            return null;

        }

        public class CustomAccessTokenProvider : IAccessTokenProvider
        {
            private readonly string _accessToken;

            public CustomAccessTokenProvider(string accessToken)
            {
                _accessToken = accessToken;
            }

            public AllowedHostsValidator AllowedHostsValidator => throw new NotImplementedException();

            public string GetAccessToken()
            {
                return _accessToken;
            }

            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
        private async static Task<IPublicClientApplication> InitializeMsalWithCache()
        {
            try
            {
                var clientId = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";
                string RedirectUri = $"msal{clientId}://auth";

                // Initialize the PublicClientApplication
                var builder = PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithWindowsEmbeddedBrowserSupport()
                        .WithRedirectUri(RedirectUri);

                builder = AuthenticationServcie.AddPlatformConfiguration(builder);

                var pca = builder.Build();

                await AuthenticationServcie.RegisterMsalCacheAsync(pca.UserTokenCache);

                return pca;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                throw;
            }

        }


        static Lazy<Task<IPublicClientApplication>> _pca;
        static string _userIdentifier = string.Empty;
        static string[] scopes = ["User.Read"];
        /// <summary>
        /// Attempts to get a token interactively using the device's browser.
        /// </summary>
        private static async Task<AuthenticationResult> GetTokenInteractivelyAsync()
        {
            var pca = await _pca.Value;

            var result = await pca.AcquireTokenInteractive(scopes)
                .ExecuteAsync();

            // Store the user ID to make account retrieval easier
            _userIdentifier = result.Account.HomeAccountId.Identifier;
            return result;

        }
        public static async Task<AuthenticationResult> GraphUserInformationAsync()
        {
            try
            {
                /*
                    pca => public client app -> user for all micrsoft accounts.
                    confidental client => use so that this application is already registered and allow to 
                    access the federation or tenant. =>  this is also useful for accessing almost everything in azure using managed indetities : TODO: learn this...
                 */

                _pca = new Lazy<Task<IPublicClientApplication>>(InitializeMsalWithCache);

                
                AuthenticationResult token = await GetTokenInteractivelyAsync();

                AppService.Dispatcher?.TryEnqueue(() =>
                {
                    IntPtr hWnd = Windowing.FindWindow(null, nameof(CreateBackup));
                    if (hWnd != IntPtr.Zero)
                    {
                        Windowing.Center(hWnd);
                    }
                });

                return token;

                #region DeviceAndConfidential 

                //HttpClient httpClient = new();

                //var tenantId = "f0d59e50-f344-4cbc-b58a-37a7ffc5a17f";
                //var clientId = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";


                //var options = new DeviceCodeCredentialOptions
                //{
                //    ClientId = clientId,
                //    TenantId = tenantId,
                //    DeviceCodeCallback = (code, cancellation) =>
                //    {
                //        Console.WriteLine(code.Message);
                //        return Task.FromResult(0);
                //    }
                //};

                //var deviceCodeCredential = new DeviceCodeCredential(options);
                //var graphClient = new GraphServiceClient(deviceCodeCredential, scopes);

                //var user = await graphClient.Me.GetAsync();
                //ExceptionLogger.LogInformation(JsonConvert.SerializeObject(user));

                //Console.WriteLine($"Hello, {user.DisplayName}");
                // Configure the MSAL client as a confidential client
                /***********    workf for getting federation users. ****************/
                //IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                //    .Create(clientId)
                //    .WithTenantId(tenantId)
                //    .WithClientSecret(clientSecret)
                //    .Build();


                //var authResult = await confidentialClientApplication.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();

                //// Initialize Graph client with custom authentication provider
                //using var graphRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users");
                //graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                //var graphResponseMessage = await httpClient.SendAsync(graphRequest);
                //graphResponseMessage.EnsureSuccessStatusCode();


                //var content = await graphResponseMessage.Content.ReadAsStringAsync();

                //Initialize Graph client with custom authentication provider

                #endregion

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                throw;
            }
        }
        //machine stuff below 
        public static Task<WindowsIdentity> GetPrincipalUser()
        {


            IntPtr hProcess = GetCurrentProcess();
            IntPtr hToken;

            if (OpenProcessToken(hProcess, TOKEN_READ, out hToken))
            {
                // Use the WindowsIdentity class to get user information
                WindowsIdentity winId = new WindowsIdentity(hToken);
                //var securityIdentifier = new SecurityIdentifier(winId.User.Value.ToString());
                //var sidBytes = new byte[securityIdentifier.BinaryLength];
                //securityIdentifier.GetBinaryForm(sidBytes, 0);
                SecurityIdentifier sid = new SecurityIdentifier(winId.User.Value.ToString());

                // Translate the SID to a NTAccount
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                string accountName = account.ToString();

                // Split domain and username
                string[] parts = accountName.Split('\\');
                if (parts.Length == 2)
                {
                    string domainName = parts[0];
                    string userName = parts[1];

                    Console.WriteLine($"User: {userName}");
                    Console.WriteLine($"Domain: {domainName}");
                }
                else
                {
                    Console.WriteLine("Failed to parse the account name.");
                }

                return Task.FromResult(winId);

            }
            else
            {
                Console.WriteLine("Failed to open process token");

                return Task.FromResult(WindowsIdentity.GetCurrent()!);
            }
        }

        [DllImport("secur32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

        public class UserNames
        {
            public string Unknown { get; set; }
            public string FullyQualifiedDN { get; set; }
            public string SamCompatible { get; set; }
            public string Display { get; set; }
            public string UniqueId { get; set; }
            public string Canonical { get; set; }
            public string UserPrincipal { get; set; }
            public string CanonicalEx { get; set; }
            public string ServicePrincipal { get; set; }
            public string DnsDomain { get; set; }
        }
        public static class NameFormats
        {
            public const int NameUnknown = 0;
            public const int NameFullyQualifiedDN = 1;
            public const int NameSamCompatible = 2;
            public const int NameDisplay = 3;
            public const int NameUniqueId = 6;
            public const int NameCanonical = 7;
            public const int NameUserPrincipal = 8;
            public const int NameCanonicalEx = 9;
            public const int NameServicePrincipal = 10;
            public const int NameDnsDomain = 12;
        }
        public static UserNames GetAllUserNames()
        {
            var userNames = new UserNames();
            uint size = 1024;
            StringBuilder name = new StringBuilder((int)size);

            userNames.Unknown = GetUserName(NameFormats.NameUnknown, name, ref size);
            userNames.FullyQualifiedDN = GetUserName(NameFormats.NameFullyQualifiedDN, name, ref size);
            userNames.SamCompatible = GetUserName(NameFormats.NameSamCompatible, name, ref size);
            userNames.Display = GetUserName(NameFormats.NameDisplay, name, ref size);
            userNames.UniqueId = GetUserName(NameFormats.NameUniqueId, name, ref size);
            userNames.Canonical = GetUserName(NameFormats.NameCanonical, name, ref size);
            userNames.UserPrincipal = GetUserName(NameFormats.NameUserPrincipal, name, ref size);
            userNames.CanonicalEx = GetUserName(NameFormats.NameCanonicalEx, name, ref size);
            userNames.ServicePrincipal = GetUserName(NameFormats.NameServicePrincipal, name, ref size);
            userNames.DnsDomain = GetUserName(NameFormats.NameDnsDomain, name, ref size);

            return userNames;
        }

        private static string GetUserName(int nameFormat, StringBuilder name, ref uint size)
        {
            name.Clear();
            size = 1024;
            if (GetUserNameEx(nameFormat, name, ref size))
            {
                return name.ToString();
            }
            else
            {
                return $"Error: {Marshal.GetLastWin32Error()}";
            }
        }
        #endregion

        public async Task GetUserProfilePicture(AuthenticationResult result)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", result.AccessToken);

                var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value");
                if (response.IsSuccessStatusCode)
                {
                    var pictureStream = await response.Content.ReadAsStreamAsync();
                    string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath);
                    string imagePath = Path.Combine(destinationFolderPath, "ms_profile.png");

                    // Save the picture stream to a file or process it as needed
                    using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
                    {
                        await pictureStream.CopyToAsync(fileStream);
                    }
                }

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                throw;
            }

        }
        public async Task<FireBrowserWinUi3MultiCore.User> GetUserInformationAsync()
        {
            uint size = 1024;
            StringBuilder name = new StringBuilder((int)size);
            FireBrowserWinUi3MultiCore.User user;

            try
            {
                /*  maybe use downline if we want to caputre our tenant users or end user's machine info.  IE: Dizzle\fizzl  => me on my machine and SID...
                 *  //var principal = GetPrincipalUser(); // machine stuff 
                 *  //var principal = DisplayCurrentUserInformation().ConfigureAwait(false);
                 */

                await AppService.MsalService.SignInAsync();

                var azGraphUser =  await AppService.GraphService.GetUserInfoAsync(); 

                if (azGraphUser is Microsoft.Graph.Models.User auth)
                {
                    user = AuthService.CurrentUser ?? new();
                    user.Email = auth.UserPrincipalName;;

                    //try
                    //{
                    //    await GetUserProfilePicture(auth);
                    //}
                    //catch {; } // do nothing is a error is thrown.  move on maybe do something later. 

                    if (GetUserNameEx(NameFormats.NameDisplay, name, ref size))
                    {
                        user.WindowsUserName = name.ToString();
                    }

                    return user;
                }

            }
            catch (MsalClientException)
            {
                return null;
                throw;
            }
            catch (Exception)
            {
                return null;
                throw;
            }

            return null;
        }

        public class ResponseAZFILE(string blobName, object sasUrl)
        {
            public string FileName => blobName;
            public object Url => sasUrl.ToString();

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public async Task InsertOrUpdateEntityAsync(string tableName, string email, string blobUrl, string blobName, string winUserName)
        {
            try
            {
                var serviceClient = new TableServiceClient(ConnString);
                var tableClient = serviceClient.GetTableClient(tableName);

                // Create the table if it doesn't exist
                await tableClient.CreateIfNotExistsAsync();

                var entity = new UserEntity
                {
                    PartitionKey = "Users",
                    RowKey = Guid.NewGuid().ToString(),
                    Email = email,
                    BlobUrl = blobUrl,
                    BlobName = blobName,
                    WindowUserName = winUserName
                };

                await tableClient.UpsertEntityAsync(entity);
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                throw;
            }
        }

        public List<UserEntity> GetUploadFileByUser(string email)
        {
            try
            {
                var serviceClient = new TableServiceClient(ConnString);
                var tableClient = serviceClient.GetTableClient(TableName);
                // Create the table if it doesn't exist
                var files = new List<UserEntity>();

                foreach (var page in tableClient.Query<UserEntity>().Where(item => item.Email == email).ToList())
                {
                    files.Add(page);
                }

                return files;

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                throw;
            }

        }
        public async Task<ResponseAZFILE> UploadAndStoreFile(string blobName, IRandomAccessStream fileStream, FireBrowserWinUi3MultiCore.User fireUser)
        {
            try
            {
                var result = await UploadFileToBlobAsync(blobName, fileStream);

                if (result is not null)
                    await InsertOrUpdateEntityAsync(TableName, fireUser.Email ?? fireUser.WindowsUserName, result.Url.ToString(), blobName, fireUser.WindowsUserName);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                return new ResponseAZFILE(ex.StackTrace, ex.Message!);

            }



        }

        public async Task<ResponseAZFILE> UploadFileToBlobAsync(string blobName, IRandomAccessStream fileStream)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(ConnString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("firebackups");
                var response = new object();
                var fileName = Guid.NewGuid().ToString() + "_" + blobName;
                // Upload the file to Azure Blob Storage
                var blockBlob = container.GetBlockBlobReference(fileName);

                await blockBlob.UploadFromStreamAsync(fileStream.AsStream());
                var blob = container.GetBlockBlobReference(fileName);

                var sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1) // Token valid for 1 hour
                });

                var sasUrl = blockBlob.Uri.AbsoluteUri + sasToken; //blockBlob.Uri.AbsoluteUri

                return new ResponseAZFILE(blobName, sasUrl);

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                throw;
            }

        }
    }
}
