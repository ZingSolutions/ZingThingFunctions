using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static ZingThingFunctions.Enums;

namespace ZingThingFunctions.Models
{
    public class SimCard
    {
        [JsonProperty("id")]
        public string SimSid { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("mobileNumber")]
        public bool MobileNumber { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActivationStatus Status { get; set; }
    }
}
