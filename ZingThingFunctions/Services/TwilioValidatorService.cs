using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Twilio.Security;
using ZingThingFunctions.Services.Interfaces;

namespace ZingThingFunctions.Services
{
    public class TwilioValidatorService : ITwilioValidatorService
    {
        private RequestValidator _requestValidator;
        private ILogger _logger;
        private bool _skipValidation;

        public TwilioValidatorService(IAppSettingsService config, ILogger<TwilioValidatorService> logger)
        {
            _requestValidator = new RequestValidator(config.AppSettings.TwilioAccountAuthToken);
            _logger = logger;
            _skipValidation = config.AppSettings.IsLocalEnvironment;
        }

        public bool IsValidRequest(HttpRequest request)
        {
            if (_skipValidation)
            {
                _logger.LogTrace("in local environment will skip Twilio authentication check");
                return true;
            }

            try
            {
                var requestUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                var parameters = request.Form.Keys
                    .Select(key => new { Key = key, Value = request.Form[key] })
                    .ToDictionary(p => p.Key, p => p.Value.ToString());

                var signature = request.Headers["X-Twilio-Signature"];

                if (!_requestValidator.Validate(requestUrl, parameters, signature))
                {
                    _logger.LogTrace("invalid call to twilio endpoint detected");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "unexpected error attempting to validate Twilio request");
                return false;
            }
        }
    }
}
