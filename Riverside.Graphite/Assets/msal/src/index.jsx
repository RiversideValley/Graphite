
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Riverside.Graphite</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/animate.css/4.1.1/animate.min.css" />
    <style>
        body {
            font-family: 'Roboto', sans-serif;
            background-color: #f5f5f5;
            color: #333;
        }

        header {
            background-color: #6a1b9a; /* Purple */
            color: #fff;
            padding: 20px;
            text-align: center;
        }

        footer {
            padding: 10px 0;
            text-align: center;
            position: fixed;
            bottom: 0;
            width: 100%;
        }
    </style>
    <script src="https://alcdn.msauth.net/browser/2.15.0/js/msal-browser.min.js"></script>
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js" integrity="sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz" crossorigin="anonymous"></script>
    <script>
        const msalConfig = {
            auth: {
                clientId: 'edfc73e2-cac9-4c47-a84c-dedd3561e8b5',
                authority: 'https://login.microsoftonline.com/common',
                redirectUri: 'https://account.microsoft.com/profile/',
                postLogoutRedirectUri: "https://www.bing.com"
            },
            cache: {
                cacheLocation: "sessionStorage", // This configures where your cache will be stored
                storeAuthStateInCookie: true, // Set this to "true" if you are having issues on IE11 or Edge
                secureCookies: false,
            },
            system: {
                iframeHashTimeout: 10000,
                loggerOptions: {
                    loggerCallback: (level, message, containsPii) => {
                        if (containsPii) {
                            return;
                        }
                        switch (level) {
                            case msal.LogLevel.Error:
                                console.error(message);
                                return;
                            case msal.LogLevel.Info:
                                console.info(message);
                                return;
                            case msal.LogLevel.Verbose:
                                console.debug(message);
                                return;
                            case msal.LogLevel.Warning:
                                console.warn(message);
                                return;
                        }
                    },
                },
            },
        };

        const loginRequest = {
            scopes: [
                "email",
                "profile",
                "User.Read",
                "Directory.Read.All",
            ],
            postLogoutRedirectUri: "http://fireapp.msal/main.html",
            forceRefresh: false,
        };

        let accountId = "";
        let accountName = "";

        const myMsal = new msal.PublicClientApplication(msalConfig);

        myMsal.addEventCallback((event) => {
            if (event.eventType === msal.EventType.LOGIN_SUCCESS && event.payload) {
                const authResult = event.payload;
                msalInstance.setActiveAccount(authResult.account);
            }
        });

        function handleResponse(response) {
            if (response !== null) {
                accountId = response.account.homeAccountId;
                // Display signed-in user content, call API, etc.
            } else {
                // In case multiple accounts exist, you can select
                const currentAccounts = myMsal.getAllAccounts();

                if (currentAccounts.length === 0) {
                    // no accounts signed-in, attempt to sign a user in
                    myMsal.loginRedirect(loginRequest);
                } else if (currentAccounts.length > 1) {
                    document.getElementById("userName").innerHTML = currentAccounts[0].user;
                    // Add choose account code here
                } else if (currentAccounts.length === 1) {
                    accountId = currentAccounts[0].homeAccountId;
                }
            }
        }
        async function signOut() {

            const logoutRequest = {
                account: myMsal.getAccountByHomeId(accountId),
                mainWindowRedirectUri: "https://fireapp.msal/main.html",
            };

            await myMsal.logoutPopup(logoutRequest);
        }
        function signIn() {
            myMsal.handleRedirectPromise().then(handleResponse).then(user => { localStorage.setItem("msalToken", user.AccessToken) }).catch(error => {
                console.error(error);
            });;
        }
    </script>
    <script async src="https://cse.google.com/cse.js?cx=c508042b055904c98">
    </script>
</head>
<body>
    <header class="p-3 text-bg-dark animate__animated animate__fadeInDown">
        <div class="container">
            <div class="d-flex flex-wrap align-items-center justify-content-center justify-content-lg-start">
                <a href="/" class="d-flex align-items-center mb-2 mb-lg-0 text-white text-decoration-none">
                    <svg class="bi me-2" width="40" height="32" role="img" aria-label="Bootstrap"><use xlink:href="#bootstrap"></use></svg>
                </a>

                <ul class="nav col-12 col-lg-auto me-lg-auto mb-2 justify-content-center mb-md-0">
                    <li><a href="#" class="nav-link px-2 text-secondary">Home </a></li>
                    <li><a href="https://apps.microsoft.com/detail/9pcn40xxvcvb?hl=en-us&gl=US" class="nav-link px-2 text-white">Download</a></li>
                    <li><a href="https://fireapp.msal/notfound.html" class="nav-link px-2 text-white">Pricing</a></li>
                    <li><a href="#" class="nav-link px-2 text-white">FAQs</a></li>
                    <li><a href="https://firebrowser-official.vercel.app/home" class="nav-link px-2 text-white">About</a></li>
                </ul>
                
                

                <div class="text-end">
                    <button type="button" class="btn btn-outline-light me-2" onclick="javascript:signIn()">Login</button>
                    <button type="button" class="btn btn-warning" onclick="javascript:signOut()">LogOut</button>
                </div>
            </div>
        </div>
    </header>
    
    <div class="container justify-content-center align-items-center mt-md-0 mb-md-0 animate__animated animate__fadeIn">
        <div class="row  mx-2 my-2">
            
            <div class="col-md-4 col-sm-4">
                <div class="card" style="width: 24rem;">
                    <img src="https://uploads.sitepoint.com/wp-content/uploads/2012/11/10_tn1.jpg" alt="Riverside.Graphite" class="animate__animated animate__zoomIn card-img-top">
                    <div class="card-body">
                        <h3 class="card-title">Welcome to Riverside.Graphite</h3>
                        <p class="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                        <a href="https://bing.com" class="btn btn-primary">Bing</a>
                    </div>
                </div>
            </div>
            <div class="col-md-6 col-sm-4">
                <h6>Google Search</h6>
                <div class="gcse-search"></div>
            </div>
            
        </div>
        <div class="row mx-2 my-2">
            <h2 class="animate__animated animate__zoomIn">Greetings..</h2>
        </div>
    </div>
    <footer class="animate__animated animate__fadeInUp footer">
        &copy; 2024 Riverside.Graphite. All rights reserved.
    </footer>


</body>
</html>
