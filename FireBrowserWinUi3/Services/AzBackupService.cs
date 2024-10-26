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
        #region StorageProcedures
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

                var azGraphUser = await AppService.MsalService.GetUserAccountAsync();

                if (azGraphUser is IAccount auth)
                {
                    user = AuthService.CurrentUser ?? new();
                    user.Email = auth.Username;

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
                var fileName = blobName;
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
        #endregion

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

    }
}
