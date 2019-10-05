using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using Thoughtpost.Helpers;

namespace TwitterBot
{
    public class TwitterMessageModel : TableEntity
    {
        public TwitterMessageModel()
        {
            this.SenderId = "";
            this.Time = DateTime.Now;
        }

        public TwitterMessageModel(dynamic msg)
        {
            this.SenderId = "";
            this.Time = DateTime.Now;
            this.Content = Newtonsoft.Json.JsonConvert.SerializeObject(msg);

            if ( Dynamics.HasProperty( msg, "follow_events") )
            {
                if (msg.follow_events[0].type == "follow")
                {
                    this.MessageType = "follow";

                    this.SenderId = msg.follow_events[0].source.id;
                    this.RecipientId = msg.follow_events[0].target.id;


                    double d = double.Parse(msg.follow_events[0].created_timestamp.ToString());
                    this.Time = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(d);
                }
            }

            if (Dynamics.HasProperty(msg, "direct_message_events"))
            {
                this.MessageType = "dm";

                this.SenderId = msg.direct_message_events[0].message_create.sender_id;
                this.RecipientId = msg.direct_message_events[0].message_create.target.recipient_id;
                this.Text = msg.direct_message_events[0].message_create.message_data.text;

                double d = double.Parse(msg.direct_message_events[0].created_timestamp.ToString());
                this.Time = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(d);

                this.MessageId = msg.direct_message_events[0].id.ToString();

                var mdata = msg.direct_message_events[0].message_create.message_data;

                if (Dynamics.HasProperty(mdata, "quick_reply_response"))
                {
                    this.Payload = mdata.quick_reply_response.metadata;
                }

                if (Dynamics.HasProperty(mdata, "attachment"))
                {
                    this.Attachments = new List<TwitterMessageAttachmentModel>();
                    TwitterMessageAttachmentModel a = new TwitterMessageAttachmentModel(msg);
                    this.Attachments.Add(a);
                }
            }


            this.PartitionKey = this.SenderId;

            this.RowKey = this.Time.Ticks.ToString();

            this.SenderProfile = new TwitterProfileModel(this.SenderId, msg);

        }

        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public string MessageId { get; set; }
        public DateTime Time { get; set; }
        public string Content { get; set; }
        public string Text { get; set; }
        public string Payload { get; set; }
        public string MessageType { get; set; }


        public TwitterProfileModel SenderProfile { get; set; }
        public List<TwitterMessageAttachmentModel> Attachments { get; set; }
    }

    public class TwitterMessageModelList
    {
        public List<TwitterMessageModel> Messages { get; set; }
    }

}
