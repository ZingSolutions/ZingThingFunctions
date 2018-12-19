namespace ZingThingFunctions.Models
{
    public class AppSettings
    {
        public bool IsLocalEnvironment { get; internal set; }
        public string TwilioAccountSid { get; internal set; }
        public string TwilioAccountAuthToken { get; internal set; }
        public string CosmosConnectionString { get; internal set; }
        public string CosmosDatabaseName { get; internal set; }
    }
}
