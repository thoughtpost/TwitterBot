using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using Thoughtpost.Helpers;

namespace TwitterBot
{
    public class TwitterProfileModel : TableEntity
    {
        public TwitterProfileModel()
        {

        }

        public TwitterProfileModel(string id, dynamic msg)
        {
            Initialize(id, msg);
        }

        public void Initialize(string id, dynamic msg)
        {
            this.ID = id;

            this.PartitionKey = "twitter";
            this.RowKey = this.ID;

            string sid = id.ToString();
            dynamic users = msg.users;

            if (Dynamics.HasProperty(users, sid))
            {
                var values = users.ToObject<Dictionary<string, object>>();
                dynamic user = Dynamics.GetObjectProperty(values, sid);

                string name = user.name;
                string[] nameparts = name.Split(' ');

                if (nameparts.Length > 0) this.FirstName = nameparts[0];
                if (nameparts.Length > 1) this.LastName = nameparts[1];

                this.ProfilePicUrl = user.profile_image_url;
            }
            else
            {
                // Might be follow event
                if (Dynamics.HasProperty(msg, "follow_events"))
                {
                    dynamic follow_events = msg.follow_events;
                    foreach (dynamic evt in follow_events)
                    {
                        if (evt.type == "follow")
                        {
                            dynamic user = evt.source;

                            string name = user.name;
                            string[] nameparts = name.Split(' ');

                            if (nameparts.Length > 0) this.FirstName = nameparts[0];
                            if (nameparts.Length > 1) this.LastName = nameparts[1];

                            this.ProfilePicUrl = user.profile_image_url;

                            break;
                        }
                    }
                }
            }
        }

        public string ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePicUrl { get; set; }

    }

    public class TwitterProfileModelList
    {
        public List<TwitterProfileModel> Profiles { get; set; }
    }

}