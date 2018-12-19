using System.Threading.Tasks;
using Twilio.Clients;
using Twilio.Http;
using ZingThingFunctions.Services.Interfaces;

namespace ZingThingFunctions.Services
{
    public class TwilioRestClientService : ITwilioRestClient
    {
        private ITwilioRestClient _client;

        public TwilioRestClientService(IAppSettingsService config)
        {
            _client = new TwilioRestClient(config.AppSettings.TwilioAccountSid, config.AppSettings.TwilioAccountAuthToken);
        }

        public string AccountSid => _client.AccountSid;
        public string Region => _client.Region;
        public HttpClient HttpClient => _client.HttpClient;
        public Response Request(Request request) => _client.Request(request);
        public Task<Response> RequestAsync(Request request) => _client.RequestAsync(request);
    }
}
