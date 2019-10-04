using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.StaticFiles;

using Thoughtpost.Helpers;

namespace TwitterBot
{
    public class TwitterMessageAttachmentModel : TableEntity
    {
        public TwitterMessageAttachmentModel()
        {
            this.SenderId = "UNKNOWN";
            this.Time = DateTime.Now;
        }

        public TwitterMessageAttachmentModel(dynamic msg)
        {
            this.SenderId = msg.direct_message_events[0].message_create.sender_id;
            this.RecipientId = msg.direct_message_events[0].message_create.target.recipient_id;

            double d = double.Parse(msg.direct_message_events[0].created_timestamp.ToString());
            this.Time = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(d);

            this.MessageId = msg.direct_message_events[0].id.ToString();

            var mdata = msg.direct_message_events[0].message_create.message_data;
            if (Dynamics.HasProperty(mdata, "attachment"))
            {
                this.Url = mdata.attachment.media.media_url;

                var provider = new FileExtensionContentTypeProvider();
                string contentType;
                if (!provider.TryGetContentType(this.Url, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                this.ContentType = contentType;

                string ext = System.IO.Path.GetExtension(this.Url);

                this.FileName = this.MessageId + ext;
            }

            this.PartitionKey = this.SenderId;

            this.RowKey = this.Time.Ticks.ToString();
        }

        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public string MessageId { get; set; }
        public DateTime Time { get; set; }
        public string Url { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }


}
