using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZingThingFunctions.Models
{
    public enum TemplateType
    {
        WtfReply,
        PungMeReply,
        SetupCustomAction
    }

    public class MessageTemplate
    {
        [JsonProperty("templateType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TemplateType TemplateType { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }

        [JsonProperty("forSimSid")]
        public string ForSimSid { get; set; }
    }
}
