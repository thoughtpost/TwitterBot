using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thoughtpost.Twitter.Models
{

    public class Button
    {
        public Button() { this.Type = "web_url"; }

        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("label")]
        public string Label { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
