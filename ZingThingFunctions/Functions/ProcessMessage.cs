using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;
using ZingThingFunctions.Models;
using ZingThingFunctions.Services.Interfaces;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions.Functions
{
    public static class ProcessMessage
    {
        [FunctionName(nameof(ProcessMessage))]
        public static async Task Run([CosmosDBTrigger(
            databaseName: AppSettings.ZingDatabaseNameEnvVarName,
            collectionName: CollectionNames.IncomingMessages,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString),
            LeaseCollectionName = CollectionNames.IncomingMessages,
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
            foreach (var docx in docs)
            {
                try
                {

                    var doc = (await cosmosClient.ReadDocumentAsync<IncomingMessage>(
                        docx.SelfLink,
                        new RequestOptions()
                        {
                            PartitionKey = new PartitionKey(docx.GetPropertyValue<string>("from"))
                        })).Document;

                    var regCollectionUri = UriFactory.CreateDocumentCollectionUri(
                        config.AppSettings.ZING_DATABASE_NAME,
                        CollectionNames.Registrations);

                    var linkedSim = cosmosClient.CreateDocumentQuery<RegistrationItem<SimCard>>(
                        regCollectionUri,
                        new FeedOptions()
                        {
                            PartitionKey = new PartitionKey(typeof(SimCard).Name)
                        })
                        .Where(e => e.Item.UserNumber == doc.From)
                        .AsEnumerable()?
                        .FirstOrDefault();

                    if (linkedSim != null)
                    {
                        if (linkedSim.Item.Status != ActivationStatus.Active)
                        {
                            log.LogWarning($"incoming message received from {doc.From}. linked SIM: {linkedSim.Id} is marked as {linkedSim.Item.Status.ToString()}. message was: {doc.Body ?? ""}");
                            continue;
                        }

                        log.LogInformation($"incoming message received from {doc.From} number is already linked up. message was: {doc.Body ?? ""}");

                        if (doc.Body != null && doc.Body.StartsWith("NAME ", StringComparison.OrdinalIgnoreCase))
                        {
                            //changing name, check not blank and update it now
                            var newName = doc.Body.Substring(5).Trim();
                            if (!string.IsNullOrWhiteSpace(newName))
                            {
                                log.LogInformation($"changing users name from {linkedSim.Item.UserName} to {newName} for sim {linkedSim.Id}");
                                linkedSim.Item.UserName = newName;

                                var simUri1 = UriFactory.CreateDocumentUri(
                                    config.AppSettings.ZING_DATABASE_NAME,
                                    CollectionNames.Registrations,
                                    linkedSim.Id);

                                await cosmosClient.ReplaceDocumentAsync(simUri1, linkedSim, new RequestOptions()
                                {
                                    PartitionKey = new PartitionKey(typeof(SimCard).Name)
                                });

                                //send confirmation SMS
                                await messagesToSend.AddAsync(new CreateMessageOptions(new PhoneNumber(doc.From))
                                {
                                    From = new PhoneNumber(linkedSim.Item.ControlNumber),
                                    Body = $"Ok, I'll call you {newName} from now on!"
                                });
                                continue;
                            }
                            else
                            {
                                await messagesToSend.AddAsync(new CreateMessageOptions(new PhoneNumber(doc.From))
                                {
                                    From = new PhoneNumber(linkedSim.Item.ControlNumber),
                                    Body = $"To change your name reply NAME followed by a space and what you would like to be called."
                                });
                                continue;
                            }
                        }

                        //let user know
                        await messagesToSend.AddAsync(new CreateMessageOptions(new PhoneNumber(doc.From))
                        {
                            From = new PhoneNumber(linkedSim.Item.ControlNumber),
                            Body = "You have already registered your phone, try pressing one of the buttons on the ZingThing!"
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(doc.Body))
                    {
                        log.LogWarning($"incoming message received from {doc.From} but no body");
                        continue;
                    }

                    //attempt to active using text code
                    var matchedSim = cosmosClient.CreateDocumentQuery<RegistrationItem<SimCard>>(
                        regCollectionUri,
                        new FeedOptions()
                        {
                            PartitionKey = new PartitionKey(typeof(SimCard).Name)
                        })
                        .Where(e =>
                        e.Item.ActivationCode == doc.Body.Trim() &&
                        e.Item.Status == ActivationStatus.Pending)
                        .AsEnumerable()?
                        .FirstOrDefault();

                    if (matchedSim == null)
                    {
                        log.LogWarning($"incoming message received from {doc.From} but no matching sim card found for code {doc.Body}");
                        continue;
                    }

                    //we found a match, activate it using the from number of the message.
                    matchedSim.Item.Status = ActivationStatus.Active;
                    matchedSim.Item.UserNumber = doc.From;
                    var simUri = UriFactory.CreateDocumentUri(
                        config.AppSettings.ZING_DATABASE_NAME,
                        CollectionNames.Registrations,
                        matchedSim.Id);

                    await cosmosClient.ReplaceDocumentAsync(simUri, matchedSim, new RequestOptions()
                    {
                        PartitionKey = new PartitionKey(typeof(SimCard).Name)
                    });

                    //send welcome SMS
                    await messagesToSend.AddAsync(new CreateMessageOptions(new PhoneNumber(doc.From))
                    {
                        From = new PhoneNumber(matchedSim.Item.ControlNumber),
                        Body = $"Hi {matchedSim.Item.UserName}, your phone is now linked to your ZingThing, try pressing a button on it!"
                    });
                    await messagesToSend.AddAsync(new CreateMessageOptions(new PhoneNumber(doc.From))
                    {
                        From = new PhoneNumber(matchedSim.Item.ControlNumber),
                        Body = $"If we have your name wrong, or you would like to change it, just reply NAME followed by a space and what you would like to be called."
                    });
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "unhandled error processing message");
                }
            }
        }
    }
}
