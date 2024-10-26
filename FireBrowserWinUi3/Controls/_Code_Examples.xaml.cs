using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3.Controls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class _Code_Examples : Page
    {
        public _Code_Examples()
        {
            this.InitializeComponent();
        }
        //public static async Task<AuthenticationResult> GraphUserInformationAsync()
        //{
        //    try
        //    {
        //        /*
        //            pca => public client app -> user for all micrsoft accounts.
        //            confidental client => use so that this application is already registered and allow to 
        //            access the federation or tenant. =>  this is also useful for accessing almost everything in azure using managed indetities : TODO: learn this...
        //         */

        //        _pca = new Lazy<Task<IPublicClientApplication>>(InitializeMsalWithCache);


        //        AuthenticationResult token = await GetTokenInteractivelyAsync();

        //        AppService.Dispatcher?.TryEnqueue(() =>
        //        {
        //            IntPtr hWnd = Windowing.FindWindow(null, nameof(CreateBackup));
        //            if (hWnd != IntPtr.Zero)
        //            {
        //                Windowing.Center(hWnd);
        //            }
        //        });

        //        return token;

        //        #region DeviceAndConfidential 

        //        //HttpClient httpClient = new();

        //        //var tenantId = "f0d59e50-f344-4cbc-b58a-37a7ffc5a17f";
        //        //var clientId = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";


        //        //var options = new DeviceCodeCredentialOptions
        //        //{
        //        //    ClientId = clientId,
        //        //    TenantId = tenantId,
        //        //    DeviceCodeCallback = (code, cancellation) =>
        //        //    {
        //        //        Console.WriteLine(code.Message);
        //        //        return Task.FromResult(0);
        //        //    }
        //        //};

        //        //var deviceCodeCredential = new DeviceCodeCredential(options);
        //        //var graphClient = new GraphServiceClient(deviceCodeCredential, scopes);

        //        //var user = await graphClient.Me.GetAsync();
        //        //ExceptionLogger.LogInformation(JsonConvert.SerializeObject(user));

        //        //Console.WriteLine($"Hello, {user.DisplayName}");
        //        // Configure the MSAL client as a confidential client
        //        /***********    workf for getting federation users. ****************/
        //        //IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
        //        //    .Create(clientId)
        //        //    .WithTenantId(tenantId)
        //        //    .WithClientSecret(clientSecret)
        //        //    .Build();


        //        //var authResult = await confidentialClientApplication.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();

        //        //// Initialize Graph client with custom authentication provider
        //        //using var graphRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users");
        //        //graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        //        //var graphResponseMessage = await httpClient.SendAsync(graphRequest);
        //        //graphResponseMessage.EnsureSuccessStatusCode();


        //        //var content = await graphResponseMessage.Content.ReadAsStringAsync();

        //        //Initialize Graph client with custom authentication provider

        //        #endregion

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionLogger.LogException(ex);
        //        throw;
        //    }
        //}
        //machine stuff below 
        //public async Task GetUserProfilePicture(AuthenticationResult result)
        //{
        //    try
        //    {
        //        var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization =
        //            new AuthenticationHeaderValue("Bearer", result.AccessToken);

        //        var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var pictureStream = await response.Content.ReadAsStreamAsync();
        //            string destinationFolderPath = Path.Combine(UserDataManager.CoreFolderPath);
        //            string imagePath = Path.Combine(destinationFolderPath, "ms_profile.png");

        //            // Save the picture stream to a file or process it as needed
        //            using (var fileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
        //            {
        //                await pictureStream.CopyToAsync(fileStream);
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionLogger.LogException(ex);
        //        throw;
        //    }
        /*
         * public static async Task DownloadBlobDirectory(CloudBlobDirectory blobDirectory, string destPath)
{
    if (!Directory.Exists(destPath))
    {
        await FileUtils.CreateDirectory(destPath);
    }

    IEnumerable<IListBlobItem> blobs = await ListBlobsFlat(blobDirectory);
    var tasks = new List<Task>();

    using (var md5 = new MD5CryptoServiceProvider())
    {
        foreach (var blobItem in blobs)
        {
            var blob = (CloudBlockBlob)blobItem;
            await blob.FetchAttributesAsync();
            string relativePath = GetLocalRelativePath(blob, blobDirectory);
            var localFilePath = Path.Combine(destPath, relativePath);
            string dirPath = Path.GetDirectoryName(localFilePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            if (File.Exists(localFilePath))
            {
                string hash = ComputeMd5Hash(md5, localFilePath);
                if (hash == blob.Properties.ContentMD5)
                {
                    continue;
                }
            }
            tasks.Add(blob.DownloadToFileAsync(localFilePath, FileMode.Create));
        }
    }
    await Task.WhenAll(tasks);
}
         */
    }
}
