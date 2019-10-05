using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Thoughtpost.Twitter;
using Thoughtpost.Twitter.Models;
using Thoughtpost.Azure;

namespace TwitterBot.Controllers
{
    [Route("[controller]")]
    [Controller]
    public class TwitterController : Controller
    {
        public TwitterController(
            IConfiguration configuration,
            ILogger<WebhookController> logger)
        {
            this.Configuration = configuration;
            this.Logger = logger;

            this.App = new TwitterApp(this.Configuration);
            this.App.UserAccessToken = this.App.AdminUserAccessToken;
            this.App.UserAccessTokenSecret = this.App.AdminUserAccessTokenSecret;
        }

        public IConfiguration Configuration { get; set; }
        public ILogger Logger { get; set; }
        public TwitterApp App { get; set; }

        [HttpGet]
        [Route("~/setwebhook")]
        public async Task<IActionResult> SetWebhook()
        {
            string responseSet = await this.App.SetWebhook(this.App.WebhookUrl);

            string responseSub = await this.App.SubscribeToWebhook();

            ViewBag.AppName = this.App.AppName;

            return View();
        }

        [HttpGet]
        [Route("~/deletewebhook")]
        public async Task<IActionResult> DeleteWebhook()
        {
            string webhook = await this.App.GetWebhook();

            string responseSet = await this.App.DeleteWebhook(this.App.WebhookUrl);

            string responseSub = await this.App.SubscribeToWebhook();

            ViewBag.AppName = this.App.AppName;

            return View();
        }

        [HttpGet]
        [Route("~/tweet/{message}")]
        public async Task<IActionResult> Tweet(string message)
        {
            ViewBag.Tweet = message;

            await this.App.SendTweet(message);

            return View();
        }


        [HttpGet]
        [Route("~/directmessage/{id}/{message}")]
        public async Task<IActionResult> DirectMessage(string id, string message)
        {
            ViewBag.Message = message;

            MessageData mdata = new MessageData();
            mdata.Text = message;

            await this.App.SendDirectMessage(id, mdata);

            return View();
        }

        [HttpGet]
        [Route("~/profiles")]
        public async Task<IActionResult> Profiles()
        {
            StorageHelper<TwitterProfileModel> storageProfiles = new StorageHelper<TwitterProfileModel>(this.Configuration);

            TwitterProfileModelList profileList = new TwitterProfileModelList();
            profileList.Profiles = await storageProfiles.Get<TwitterProfileModel>( "twitter", "profiles" );

            return View(profileList);
        }

    }
}
