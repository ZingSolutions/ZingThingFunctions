namespace ZingThingFunctions.Models
{
    public class AppSettings
    {
        public bool IsLocalEnvironment { get; internal set; }
        public string TwilioAccountSid { get; internal set; }
        public string TwilioAccountAuthToken { get; internal set; }
        public string CosmosConnectionString { get; internal set; }
        public string ZING_DATABASE_NAME { get; internal set; }
        public string LEASES_DATABASE_NAME { get; internal set; }

        public const string ZingDatabaseNameEnvVarName = "%" + nameof(ZING_DATABASE_NAME) + "%";
        public const string LeasesDatabaseNameEnvVarName = "%" + nameof(LEASES_DATABASE_NAME) + "%";
    }
}
