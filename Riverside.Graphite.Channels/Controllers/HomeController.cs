using FireCore.Models;
using FireCore.Services;
using FireCore.Services.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;

namespace FireCore.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;
		private readonly IHubCommander? _commander;

		public HomeController(IConfiguration configuration, IHubCommander commander, ILogger<HomeController> logger)
		{
			_logger = logger;
			_commander = commander;
			_configuration = configuration;

		}

		public IActionResult Index()
		{

			ViewData["GraphApiResult"] = User.GetDisplayName();
			return View();
		}

		[HttpGet("users/all")]
		public IActionResult GetallMsalUsers()
		{

			var users = AzureChat.ConnectedIds.Select(t => t.Value).ToList(); // _commander?.MsalCurrentUsers;
			return new JsonResult(users!.Select(x => x).ToArray());
		}


		public IActionResult Privacy()
		{
			return View();
		}

		[AllowAnonymous]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		[AllowAnonymous]
		public IActionResult ErrorWithMessage(string message, string debug)
		{
			return View();
		}
	}
}
