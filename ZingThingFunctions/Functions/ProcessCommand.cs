using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;
using ZingThingFunctions.Extensions;
using ZingThingFunctions.Models;
using ZingThingFunctions.Services.Interfaces;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions.Functions
{
    public static class ProcessCommand
    {
        /// <summary>
        /// command prefixes, in uppercase
        /// </summary>
        private static class CommandPrefixes
        {
            public const string ButtonPress = "BTN_";
            public const string SystemAction = "SYS_";
        }

        [FunctionName(nameof(ProcessCommand))]
        public static async Task Run([CosmosDBTrigger(
            databaseName: AppSettings.ZingDatabaseNameEnvVarName,
            collectionName: CollectionNames.IncomingCommands,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString),
            LeaseCollectionName = CollectionNames.IncomingCommands,
            LeaseDatabaseName = AppSettings.LeasesDatabaseNameEnvVarName,
            LeasesCollectionThroughput = LeaseCollections.DefaultThroughput,
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> docs,
            [CosmosDB(ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString))] DocumentClient cosmosClient,
            [Inject] IAppSettingsService config,
            [TwilioSms(
            AccountSidSetting = nameof(AppSettings.TwilioAccountSid),
            AuthTokenSetting = nameof(AppSettings.TwilioAccountAuthToken))] IAsyncCollector<CreateMessageOptions> messagesToSend,
            ILogger log)
        {
            if (docs == null || docs.Count == 0) return;

            log.LogInformation($"{docs.Count} commands received");
            foreach (var docx in docs)
            {
                var doc = (await cosmosClient.ReadDocumentAsync<IncomingCommand>(
                    docx.SelfLink,
                    new RequestOptions()
                    {
                        PartitionKey = new PartitionKey(docx.GetPropertyValue<string>("simSid"))
                    })).Document;

                if (doc.CommandStatus == null || !doc.CommandStatus.Equals("received", StringComparison.OrdinalIgnoreCase))
                {
                    log.LogWarning($"skipping command: {doc.CommandSid}, is in a unsupported status: {doc.CommandStatus}");
                    if (!string.IsNullOrWhiteSpace(doc.ErrorCode) || !string.IsNullOrWhiteSpace(doc.ErrorMessage))
                    {
                        log.LogWarning($"command: {doc.CommandSid} had error. code: {doc.ErrorCode} message: {doc.ErrorMessage}");
                    }
                    continue;
                }

                if (doc.CommandMode == null || !doc.CommandMode.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    log.LogWarning($"skipping command: {doc.CommandSid}, using unsupported command mode: {doc.CommandMode}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(doc.Command))
                {
                    log.LogWarning($"skipping command: {doc.CommandSid}, Command is missing");
                    continue;
                }


                var simDocUri = UriFactory.CreateDocumentUri(
                    config.AppSettings.ZING_DATABASE_NAME,
                    CollectionNames.Registrations,
                    doc.SimSid);

                SimCard sim = null;
                try
                {
                    var res = await cosmosClient.ReadDocumentAsync<RegistrationItem<SimCard>>(
                        simDocUri,
                        new RequestOptions()
                        {
                            PartitionKey = new PartitionKey(typeof(SimCard).Name)
                        });
                    sim = res.Document.Item;
                }
                catch (DocumentClientException dce) when (dce.StatusCode == HttpStatusCode.NotFound)
                {
                    //not found
                    sim = null;
                }

                if (sim == null)
                {
                    log.LogError($"skipping command: {doc.CommandSid}, no matching {typeof(SimCard).Name} card registration found");
                    continue;
                }

                switch (sim.Status)
                {
                    case ActivationStatus.Active:
                        if (string.IsNullOrWhiteSpace(sim.UserNumber))
                        {
                            log.LogError($"can process command: {doc.CommandSid}, matching sim: {sim.SimSid} is Active but UserNumber is missing");
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(sim.ControlNumber))
                        {
                            log.LogError($"can process command: {doc.CommandSid}, matching sim: {sim.SimSid} is Active but ControlNumber is missing");
                            continue;
                        }
                        break; //all good carry on
                    case ActivationStatus.Pending:
                        //user has not registered yet, don't know what number linked to
                        log.LogWarning($"cant process command: {doc.CommandSid}, matching sim: {sim.SimSid} not yet activated");
                        //TODO: work out if we can send command to device to let know not yet registered, for red light feedback
                        await UpdateSimRegistrationRunCount(cosmosClient, simDocUri, nameof(RunCounts.NotActivated));
                        continue;
                    default:
                        log.LogError($"skipping command: {doc.CommandSid}, linked sim is in unexpected state: {sim.Status}");
                        continue;
                }

                switch (doc.Command.Trim().ToUpper())
                {
                    case CommandPrefixes.ButtonPress + "1":
                        await messagesToSend.AddAsync(GenerateMessage(cosmosClient, config.AppSettings.ZING_DATABASE_NAME, sim, TemplateType.WtfReply));
                        await UpdateSimRegistrationRunCount(cosmosClient, simDocUri, nameof(RunCounts.Wtf));
                        break;
                    case CommandPrefixes.ButtonPress + "2":
                        await messagesToSend.AddAsync(GenerateMessage(cosmosClient, config.AppSettings.ZING_DATABASE_NAME, sim, TemplateType.PungMeReply));
                        await UpdateSimRegistrationRunCount(cosmosClient, simDocUri, nameof(RunCounts.PungMe));
                        break;
                    case CommandPrefixes.ButtonPress + "3":
                        await messagesToSend.AddAsync(ProcessCustomAction(cosmosClient, config.AppSettings.ZING_DATABASE_NAME, sim));
                        await UpdateSimRegistrationRunCount(cosmosClient, simDocUri, nameof(RunCounts.CustomAction));
                        break;
                    case CommandPrefixes.SystemAction + "LOW_PWR":
                        //TODO: handle low power
                        log.LogError($"{doc.Command} requested for command: {doc.CommandSid} but not yet implemented");
                        break;
                    default:
                        log.LogWarning($"skipping command: {doc.CommandSid}, Command not mapped: {doc.Command}");
                        break;
                }
            }
        }

        private static CreateMessageOptions ProcessCustomAction(DocumentClient client, string dbName, SimCard sim)
        {
            //TODO: process custom action
            return GenerateMessage(client, dbName, sim, TemplateType.SetupCustomAction);
        }

        private static CreateMessageOptions GenerateMessage(DocumentClient client, string dbName, SimCard sim, TemplateType templateType)
        {
            var match = client.GetMatchingRegistrationItems<MessageTemplate>(dbName, (e) =>
            {
                return e.Item.TemplateType == templateType && e.Item.ForSimSid == sim.SimSid;
            }).FirstOrDefault();

            if (match == null)
            {
                match = client.GetMatchingRegistrationItems<MessageTemplate>(dbName, (e) =>
                {
                    return e.Item.TemplateType == templateType && e.Item.ForSimSid == null;
                }).FirstOrDefault();
            }

            if (match == null)
            {
                throw new ArgumentException($"no matching template found. templateType: {templateType}, simSid: {sim.SimSid}");
            }

            var msgBody = match.Template
                .Replace("{userName}", sim.UserName)
                .Replace("{name}", sim.UserName)
                .Replace("{za-number}", "+441494372405");

            CreateMessageOptions msg = new CreateMessageOptions(new PhoneNumber(sim.UserNumber));
            msg.From = new PhoneNumber(sim.ControlNumber);
            msg.Body = msgBody;
            return msg;
        }


        private static async Task UpdateSimRegistrationRunCount(DocumentClient client, Uri simDocumentUri, string countPropName)
        {
            var prop = typeof(RunCounts).GetProperties()
                .Where(e => e.Name == countPropName).FirstOrDefault();

            if (prop == null)
                throw new ArgumentException("property not found", nameof(countPropName));

            int attemptCount = 1;
            const int maxWriteAttepmts = 4;
            while (true)
            {
                var res = await client.ReadDocumentAsync<RegistrationItem<SimCard>>(
                    simDocumentUri,
                    new RequestOptions() { PartitionKey = new PartitionKey(typeof(SimCard).Name) });

                var newValue = (int)prop.GetValue(res.Document.Item.RunCounts) + 1;
                prop.SetValue(res.Document.Item.RunCounts, newValue);

                try
                {
                    var r = await client.ReplaceDocumentAsync(simDocumentUri, res.Document, new RequestOptions()
                    {
                        PartitionKey = new PartitionKey(typeof(SimCard).Name),
                        AccessCondition = new AccessCondition()
                        {
                            Type = AccessConditionType.IfMatch,
                            Condition = res.Document.ETag
                        }
                    });
                    return;
                }
                catch (DocumentClientException dce) when (
                dce.StatusCode == HttpStatusCode.PreconditionFailed
                && attemptCount < maxWriteAttepmts)
                {
                    //someone else has retrieved and updated the doc before us,
                    // increase attempt count and try again
                    attemptCount += 1;
                }
            }
        }
    }
}
