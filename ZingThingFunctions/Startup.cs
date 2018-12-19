using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Twilio.Clients;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;
using ZingThingFunctions;
using ZingThingFunctions.Services;
using ZingThingFunctions.Services.Interfaces;

[assembly: WebJobsStartup(typeof(Startup))]
namespace ZingThingFunctions
{
    internal class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder) =>
            builder.AddDependencyInjection(ConfigureServices);

        private void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddEnvironmentVariables()
                  .Build();

            services.AddLogging(); //TODO: hook up to app insights
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IAppSettingsService, ConfigurationAppSettingsService>();
            services.AddSingleton<ICosmosCollectionUriService, CosmosCollectionUriService>();
            services.AddSingleton<ITwilioValidatorService, TwilioValidatorService>();
            services.AddSingleton<ITwilioRestClient, TwilioRestClientService>();
        }
    }
}
