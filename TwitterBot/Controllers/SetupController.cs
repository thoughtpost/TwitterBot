using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Thoughtpost.Twitter;

namespace TwitterBot.Controllers
{
    [Route("[controller]")]
    [Controller]
    public class SetupController : Controller
    {
        public SetupController(
            IConfiguration configuration,
            ILogger<WebhookController> logger)
        {
            this.Configuration = configuration;
            this.Logger = logger;
            this.App = new TwitterApp(this.Configuration);
        }

        public IConfiguration Configuration { get; set; }
        public ILogger Logger { get; set; }
        public TwitterApp App { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            this.App.UserAccessToken = this.App.AdminUserAccessToken;
            this.App.UserAccessTokenSecret = this.App.AdminUserAccessTokenSecret;

            string response = await this.App.SetWebhook( this.App.WebhookUrl );

            string resp2 = await this.App.SubscribeToWebhook();
            //await this.App.SendTweet("Hello, World! :)");

            return View();
        }

    }
}
