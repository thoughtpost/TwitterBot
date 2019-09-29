using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thoughtpost.Twitter.Models
{

    public class QuickReply
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("options")]
        public List<QuickReplyOption> Options { get; set; }
    }
}
