using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thoughtpost.Twitter.Models
{

    public class QuickReplyOption
    {
        [JsonProperty("label")]
        public string Label { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("metadata")]
        public string Metadata { get; set; }
    }

}
