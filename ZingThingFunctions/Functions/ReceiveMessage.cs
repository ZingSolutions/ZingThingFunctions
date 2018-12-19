using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZingThingFunctions.Extensions;
using ZingThingFunctions.Models;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions.Functions
{
    public static class ReceiveMessage
    {
        [FunctionName(nameof(ReceiveMessage))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = nameof(ReceiveMessage))]HttpRequest req,
             [CosmosDB(
            databaseName: AppSettings.ZingDatabaseNameEnvVarName,
            collectionName: CollectionNames.IncomingMessages,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString))] IAsyncCollector<IncomingMessage> docs,
            ILogger log
            )
        {

            var result = await req.DeserializeFormDataToObjectAsync<IncomingMessage>();

            if (string.IsNullOrWhiteSpace(result.MessageSid))
                throw new Exception("failed to parse body, missing MessageSid");

            if (string.IsNullOrWhiteSpace(result.From))
                throw new Exception("failed to parse body, missing From");

            await docs.AddAsync(result);
            return new AcceptedResult();
        }
    }
}
