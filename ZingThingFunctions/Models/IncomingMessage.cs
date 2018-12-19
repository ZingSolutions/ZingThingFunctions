using Newtonsoft.Json;

namespace ZingThingFunctions.Models
{
    public class IncomingMessage
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("simSid")]
        public string SmsSid { get; set; }
    }
}
