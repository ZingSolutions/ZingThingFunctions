using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Twilio.Clients;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;
using ZingThingFunctions.Extensions;
using ZingThingFunctions.Models;
using ZingThingFunctions.Services.Interfaces;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions.Functions
{
    public static class ReceiveCommand
    {
        [FunctionName(nameof(ReceiveCommand))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = nameof(ReceiveCommand))]HttpRequest req,
            [Inject] ITwilioValidatorService twilioRequestValidator,
            [Inject] ITwilioRestClient twilioRestClient,
            [CosmosDB(
            databaseName: AppSettings.ZingDatabaseNameEnvVarName,
            collectionName: CollectionNames.IncomingCommands,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString))] IAsyncCollector<IncomingCommand> docs,
            ILogger log)
        {
            if (!twilioRequestValidator.IsValidRequest(req))
                return new StatusCodeResult(StatusCodes.Status401Unauthorized);

            log.LogInformation("passed authentication check, is a valid request from twilio process it");

            var result = await req.DeserializeFormDataToObjectAsync<IncomingCommand>();

            if (string.IsNullOrWhiteSpace(result.CommandSid))
                throw new Exception("failed to parse body, missing CommandSid");

            if (string.IsNullOrWhiteSpace(result.SimSid))
                throw new Exception("failed to parse body, missing SimSid");

            await docs.AddAsync(result);
            return new OkResult();
        }
    }
}
