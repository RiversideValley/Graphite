// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FireCore.Services;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;

namespace FireCore.Controllers
{

	[ApiController]
    public class NegotiateController : ControllerBase
    {
        private const string EnableDetailedErrors = "EnableDetailedErrors";
        private readonly ServiceHubContext? _messageHubContext;
        private readonly ServiceHubContext? _chatHubContext;
        private readonly bool _enableDetailedErrors;
        
        public NegotiateController(IHubContextStore store, IConfiguration configuration)
        {
            _messageHubContext = store.MessageHubContext;
            _chatHubContext = store.ChatHubContext;
            _enableDetailedErrors = configuration.GetValue(EnableDetailedErrors, false);
            
        }

        [HttpPost("message/negotiate")]
        public Task<ActionResult> MessageHubNegotiate()
        {
			var user = Environment.UserDomainName + "@" + Environment.UserName + ".graphite.net";
            return NegotiateBase(user!, _messageHubContext!);
        }

        private async Task<ActionResult> NegotiateBase(string user, ServiceHubContext serviceHubContext)
        {
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("User ID is null or empty.");
            }

            NegotiationResponse? negotiateResponse = await serviceHubContext.NegotiateAsync(new()
            {
                UserId = user,
                EnableDetailedErrors = _enableDetailedErrors
            });

            return new JsonResult(new Dictionary<string, string>()
            {
                { "url", negotiateResponse.Url! },
                { "accessToken", negotiateResponse.AccessToken! }
            });
        }
    }
}