using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Internal;

using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;

using Thoughtpost.Twitter;
using Thoughtpost.Twitter.Models;
using Thoughtpost.Azure;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitterBot.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        public WebhookController(
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
        public IActionResult Webhook(string crc_token)
        {
            this.Logger.LogDebug("Token received = " + crc_token);

            string json = "{}";

            if (string.IsNullOrEmpty(crc_token) == false)
            {
                json = this.App.AcceptChallenge(crc_token);
            }

            this.Logger.LogDebug("Response = " + json);

            return Content(json, "application/json");
        }


        [HttpPost]
        [ActionName("Webhook")]
        public async Task<IActionResult> WebhookPost()
        {
            string json = "";
            dynamic msg = GetRequestBodyObject(out json);

            this.Logger.LogDebug(json);

            TwitterMessageModel model = new TwitterMessageModel(msg);

            await SaveMessage(model);

            await Respond(model);

            return await Task.FromResult(Ok());
        }

        protected async Task SaveMessage(TwitterMessageModel model)
        {
            if (model == null) return;
            if (string.IsNullOrEmpty(model.SenderId)) return;

            StorageHelper<TwitterMessageModel> storage = new StorageHelper<TwitterMessageModel>(this.Configuration);

            await storage.SaveToTable<TwitterMessageModel>(model, "messages");

            StorageHelper<TwitterProfileModel> storageProfiles = new StorageHelper<TwitterProfileModel>(this.Configuration);

            await storageProfiles.SaveToTable<TwitterProfileModel>(model.SenderProfile, "profiles");

            if (model.Attachments != null && model.Attachments.Count > 0)
            {
                StorageHelper<TwitterMessageAttachmentModel> storageAttachments = new StorageHelper<TwitterMessageAttachmentModel>(this.Configuration);

                await storageAttachments.SaveToTable<TwitterMessageAttachmentModel>(
                    model.Attachments[0], "attachments");

                byte[] attachmentBytes = await this.App.GetBytes(model.Attachments[0].Url);

                await storageAttachments.Save("attachments", attachmentBytes, model.Attachments[0].FileName, "");
            }
        }

        protected async Task Respond(TwitterMessageModel model)
        {
            if (model == null) return;

            if (model.MessageType == "follow")
            {
                await this.App.SendDirectMessage(model.SenderId, "Thanks for following me!");

                MessageData md = new MessageData();
                md.Text = "How would you rank this session?";
                md.QuickReply = new QuickReply();

                QuickReplyOption option1 = new QuickReplyOption() { Label = "Great", Description = "I love it", Metadata = "Great" };
                QuickReplyOption option2 = new QuickReplyOption() { Label = "Really Great", Description = "Best ever", Metadata = "Amazing" };
                QuickReplyOption option3 = new QuickReplyOption() { Label = "These go to 11", Description = "Spinal Tap Level Session", Metadata = "11" };

                md.QuickReply.Type = "options";
                md.QuickReply.Options = new List<QuickReplyOption>();
                md.QuickReply.Options.Add(option1);
                md.QuickReply.Options.Add(option2);
                md.QuickReply.Options.Add(option3);

                await this.App.SendDirectMessage(model.SenderId, md);
            }

            if ( model.MessageType == "dm")
            {
                // We sent this, ignore
                if (this.App.AdminUserAccessToken.StartsWith(model.SenderId)) return;

                await this.App.SendTweet(model.SenderProfile.FirstName + " told me, \"" + model.Text + "\". " + 
                    model.SenderProfile.ProfilePicUrl );
            }
        }

        public dynamic GetRequestBodyObject(out string json)
        {
            json = string.Empty;

            Request.EnableRewind();

            Stream req = Request.Body;
            req.Seek(0, System.IO.SeekOrigin.Begin);
            json = new StreamReader(req).ReadToEnd();

            return JsonConvert.DeserializeObject(json);
        }
    }
}
