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

            return await Task.FromResult(Ok());
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
