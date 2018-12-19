using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Twilio.Clients;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;
using ZingThingFunctions.Models;
using ZingThingFunctions.Services.Interfaces;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions
{
    public static class ReceiveMessage
    {
        [FunctionName(nameof(ReceiveMessage))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = nameof(ReceiveMessage))]HttpRequest req,
            [Inject] ITwilioValidatorService twilioRequestValidator,
            [Inject] ITwilioRestClient twilioRestClient,
            [CosmosDB(
            "%" + nameof(AppSettings.CosmosDatabaseName) + "%",
            CollectionNames.IncomingMessages,
            ConnectionStringSetting = nameof(AppSettings.CosmosConnectionString))] IAsyncCollector<IncomingMessage> docs,
            ILogger log)
        {
            if (!twilioRequestValidator.IsValidRequest(req))
                return new StatusCodeResult(StatusCodes.Status401Unauthorized);

            log.LogInformation("passed authentication check, is a valid request from twilio process it");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);

            await docs.AddAsync(new IncomingMessage() { SmsSid = "thisIsTestYo" });

            return new OkResult();
        }
    }
}
