using Newtonsoft.Json;
using System;

namespace ZingThingFunctions.Models
{
    public class IncomingMessage
    {
        [JsonProperty("id")]
        public string MessageSid { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("fromZip")]
        public string FromZip { get; set; }

        [JsonProperty("fromCity")]
        public string FromCity { get; set; }

        [JsonProperty("fromState")]
        public string FromState { get; set; }

        [JsonProperty("fromCountry")]
        public string FromCountry { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("messagingServiceSid")]
        public string MessagingServiceSid { get; set; }

        [JsonProperty("accountSid")]
        public string AccountSid { get; set; }

        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        [JsonProperty("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; }
    }
}
