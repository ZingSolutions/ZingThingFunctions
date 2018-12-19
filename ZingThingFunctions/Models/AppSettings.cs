namespace ZingThingFunctions.Models
{
    public class AppSettings
    {
        public bool IsLocalEnvironment { get; internal set; }
        public string TwilioAccountSid { get; internal set; }
        public string TwilioAccountAuthToken { get; internal set; }
        public string CosmosConnectionString { get; internal set; }
        public string ZingDatabaseName { get; internal set; }
        public string LeasesDatabaseName { get; internal set; }

        public const string ZingDatabaseNameEnvVarName = "%" + nameof(ZingDatabaseName) + "%";
        public const string LeasesDatabaseNameEnvVarName = "%" + nameof(LeasesDatabaseName) + "%";
    }
}
