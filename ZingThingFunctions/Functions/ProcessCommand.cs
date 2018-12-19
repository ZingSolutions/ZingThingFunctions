using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;
using ZingThingFunctions.Models;
using ZingThingFunctions.Services.Interfaces;
using static ZingThingFunctions.Constants.Cosmos;
using static ZingThingFunctions.Enums;

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
        public static async void Run([CosmosDBTrigger(
            databaseName: AppSettings.ZingDatabaseNameEnvVarName,
            collectionName: CollectionNames.IncomingCommands,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString),
            LeaseCollectionName = CollectionNames.IncomingCommands,
            LeaseDatabaseName = AppSettings.LeasesDatabaseNameEnvVarName,
            LeasesCollectionThroughput = LeaseCollections.DefaultThroughput,
            CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<IncomingCommand> docs,
            [CosmosDB(ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString))] DocumentClient cosmosClient,
            [Inject] ICosmosCollectionUriService collectionUris,
            [Inject] IAppSettingsService config,
            ILogger log)
        {
            if (docs == null || docs.Count == 0) return;

            log.LogInformation($"{docs.Count} commands received");
            foreach (var doc in docs)
            {
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

                var uri = UriFactory.CreateDocumentUri(
                    config.AppSettings.ZingDatabaseName,
                    CollectionNames.Registrations,
                    doc.SimSid);

                SimCard sim = null;
                try
                {
                    sim = await cosmosClient.ReadDocumentAsync<SimCard>(uri,
                    new RequestOptions() { PartitionKey = new PartitionKey(nameof(SimCard)) });

                }
                catch (DocumentClientException dce) when (dce.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    sim = null;
                }

                if (sim == null)
                {
                    log.LogError($"skipping command: {doc.CommandSid}, no matching sim card registration found");
                    continue;
                }

                switch (sim.Status)
                {
                    case ActivationStatus.Active:
                        break; //all good carry on
                    case ActivationStatus.Pending:
                        //user has not registered yet, don't know what number linked to
                        RunNotRegistered(doc, sim);
                        continue;
                    default:
                        log.LogError($"skipping command: {doc.CommandSid}, linked sim is in unexpected state: {sim.Status}");
                        continue;
                }

                switch (doc.Command.Trim().ToUpper())
                {
                    case CommandPrefixes.ButtonPress + "1":
                        RunWtf(doc, sim);
                        break;
                    case CommandPrefixes.ButtonPress + "2":
                        RunPungMe(doc, sim);
                        break;
                    case CommandPrefixes.ButtonPress + "3":
                        RunCustomAction(doc, sim);
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
    }
}
