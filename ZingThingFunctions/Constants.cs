namespace ZingThingFunctions
{
    public static class Constants
    {
        public static class Cosmos
        {
            public static class CollectionNames
            {
                public const string IncomingCommands = "zing-thing-incoming-commands";
                public const string IncomingMessages = "zing-thing-incoming-messages";
                public const string SentMessageReceipts = "zing-thing-sent-message-receipts";
                public const string Registrations = "zing-thing-registrations";
            }

            public static class LeaseCollections
            {
                public const int DefaultThroughput = 400;
            }
        }
    }
}
