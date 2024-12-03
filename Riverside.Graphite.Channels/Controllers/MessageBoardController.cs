using FireCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace FireCore.Controllers
{
	public class MessageBoardController : Controller
    {
        #region Initiliaze;

        private readonly IHubContextStore? _contextStore;
        private readonly IHubCommander? _commander;
        private readonly IConfiguration? _configuration;
        public MessageBoardController(IHubContextStore hubContextStore, IHubCommander commander, IConfiguration configuration)
        {
            _contextStore = hubContextStore;
            _commander = commander;
            _configuration = configuration;
        
        }
        #endregion;

        public async Task<IActionResult> Index()
        {
            try
            {
                return View();
            }
            catch (Exception e)
            {
                foreach (var cookie in Request.Cookies)
                { Response.Cookies.Delete(cookie.Key); }
                Console.Write(e.Message!.ToString());
            };

            return Redirect("~/");

        }
    }
}
