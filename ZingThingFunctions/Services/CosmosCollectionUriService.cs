using Microsoft.Azure.Documents.Client;
using System;
using ZingThingFunctions.Services.Interfaces;

namespace ZingThingFunctions.Services
{
    public class CosmosCollectionUriService : ICosmosCollectionUriService
    {
        public CosmosCollectionUriService(IAppSettingsService config)
        {
            RegistrationsCollectionUri = UriFactory.CreateDocumentCollectionUri(config.AppSettings.ZingDatabaseName, Constants.Cosmos.CollectionNames.Registrations);
        }

        public Uri Registrations { get; }
    }
}
