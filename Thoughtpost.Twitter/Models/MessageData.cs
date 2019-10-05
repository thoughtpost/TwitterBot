using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thoughtpost.Twitter.Models
{

    public class MessageData
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("quick_reply")]
        public QuickReply QuickReply { get; set; }
        [JsonProperty("ctas")]
        public List<Button> Buttons { get; set; }
    }


}
