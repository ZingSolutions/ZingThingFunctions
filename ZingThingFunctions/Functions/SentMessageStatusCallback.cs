using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ZingThingFunctions.Models;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions.Functions
{
    public static class SentMessageStatusCallback
    {
        [FunctionName(nameof(SentMessageStatusCallback))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = nameof(SentMessageStatusCallback))]HttpRequest req,
             [CosmosDB(
            databaseName: AppSettings.ZingDatabaseNameEnvVarName,
            collectionName: CollectionNames.SentMessageReceipts,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString))] IAsyncCollector<SentMessageReciept> docs,
            ILogger log
            )
        {
            var postData = await req.ReadAsStringAsync();
            await docs.AddAsync(new SentMessageReciept() { simSid = "SentMessageReciept", ContentType = req.ContentType, Data = postData });

            return new AcceptedResult();
        }
    }
}
