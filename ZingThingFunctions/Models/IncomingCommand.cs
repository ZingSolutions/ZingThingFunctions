using Newtonsoft.Json;

namespace ZingThingFunctions.Models
{
    public class IncomingCommand
    {
        [JsonProperty("id")]
        public string CommandSid { get; set; }

        [JsonProperty("simSid")]
        public string SimSid { get; set; }

        [JsonProperty("simUniqueName")]
        public string SimUniqueName { get; set; }

        [JsonProperty("accountSid")]
        public string AccountSid { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("commandStatus")]
        public string CommandStatus { get; set; }

        [JsonProperty("commandMode")]
        public string CommandMode { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }
    }
}
