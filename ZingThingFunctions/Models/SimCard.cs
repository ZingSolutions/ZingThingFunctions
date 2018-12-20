using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZingThingFunctions.Models
{
    public enum ActivationStatus
    {
        Pending,
        Active,
        Canceled
    }

    public class RunCounts
    {
        [JsonProperty("notActivated")]
        public int NotActivated { get; set; }

        [JsonProperty("wtf")]
        public int Wtf { get; set; }

        [JsonProperty("pungMe")]
        public int PungMe { get; set; }

        [JsonProperty("customAction")]
        public int CustomAction { get; set; }
    }


    public class SimCard
    {
        [JsonProperty("simSid")]
        public string SimSid { get; set; }

        [JsonProperty("controlNumber")]
        public string ControlNumber { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActivationStatus Status { get; set; }


        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("userNumber")]
        public string UserNumber { get; set; }

        [JsonProperty("runCounts")]
        public RunCounts RunCounts { get; set; }
    }
}
