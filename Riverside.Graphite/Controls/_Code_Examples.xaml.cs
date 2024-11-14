using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FireBrowserWinUi3.Controls
{
	/// <summary>
	/// 
	/// </summary>
	public sealed partial class _Code_Examples : Page
	{
		public _Code_Examples()
		{
			InitializeComponent();
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
         public List<string> ExtractCookies(AuthenticationResult result)
    {
        var cookies = new List<string>();
        var cookieHeader = result.AccessToken.Split('.')[0];
        var cookieParts = cookieHeader.Split(';');
        foreach (var part in cookieParts)
        {
            cookies.Add(part.Trim());
        }
        return cookies;
    }


    public async Task SetCookiesInWebView2(CoreWebView2 webView2, List<string> cookies)
    {


        //foreach (var cookie in cookies)
        //{
        //    ;
        //    webView2.CookieManager.AddOrUpdateCookie(cookie);
        //}
    }
        
   
            // doesn't work for auth into ms page, althogh a cool way of looking at the traffic
        // for each request 

        //s.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

        //s.CoreWebView2.WebResourceRequested += async (sender, args) =>
        //{


        //    if (!AppService.MsalService.IsSignedIn) return;

        //    if (FirstAttempt > 0) return;
        //    else
        //        FirstAttempt++;

        //    var body = await GetAccessTokenAsync();

        //    string script = $"localStorage.setItem('www.microsoft365.com','{body}'); ";

        //    await sender.ExecuteScriptAsync(script);

        //    var answer = await AppService.MsalService.SignInAsync();

        //    var request = args.Request;
        //    var headers = request.Headers;

        //    headers.SetHeader("Authorization", body);
        //    var cookieManager = sender.CookieManager;
        //    var cookie = cookieManager.CreateCookie("Authorization", body, "https://www.microsoft365.com", "/");
        //    cookieManager.AddOrUpdateCookie(cookie);

        //    //string ClientId = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";
        //    //string RedirectUri = "ms-appx-web://microsoft.aad.brokerplugin/edfc73e2-cac9-4c47-a84c-dedd3561e8b5";

        //    //var postData = new Dictionary<string, string>
        //    //{
        //    //    { "grant_type" , "client_credentials" },
        //    //    { "client_id", ClientId },
        //    //    { "scope", ".default" },
        //    //    { "code", body },
        //    //    { "redirect_uri", RedirectUri }
        //    //};

        //    //var uri = new Uri("https://login.microsoftonline.com/f0d59e50-f344-4cbc-b58a-37a7ffc5a17f/oauth2/v2.0/authorize");

        //    //var requestContent = new FormUrlEncodedContent(postData);

        //    //var web2req = sender.Environment.CreateWebResourceRequest(
        //    //    uri.ToString(),
        //    //    "POST",
        //    //    requestContent.ReadAsStream().AsRandomAccessStream(),
        //    //    "Content-Type: application/x-www-form-urlencoded");

        //    //sender.NavigateWithWebResourceRequest(web2req);

        //    //if (answer.AccessToken is not null)
        //    //{
        //    //    headers.SetHeader("Authorization", body);
        //    //    var cookieManager = sender.CookieManager;
        //    //    var cookie = cookieManager.CreateCookie("Authorization", body, "https://www.microsoft365.com", "/");
        //    //    cookieManager.AddOrUpdateCookie(cookie);
        //    //}

        //};

        // Navigate to the desired URL


    public async Task<string> GetAccessTokenAsync()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/f0d59e50-f344-4cbc-b58a-37a7ffc5a17f/oauth2/v2.0/token");
        var uri = "https://login.microsoftonline.com/f0d59e50-f344-4cbc-b58a-37a7ffc5a17f/oauth2/v2.0/token";
        string RedirectUri = "ms-appx-web://microsoft.aad.brokerplugin/edfc73e2-cac9-4c47-a84c-dedd3561e8b5";
        string ClientId = "edfc73e2-cac9-4c47-a84c-dedd3561e8b5";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri),
            new KeyValuePair<string, string>("scope", ".default")
        });

        request.Content = content;
        var response = await client.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        // Extract the access token from the response
        // (You might want to use a JSON parser here)
        return responseBody;
    }


         */
	}
}
