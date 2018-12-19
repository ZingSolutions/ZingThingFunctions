using Microsoft.AspNetCore.Http;

namespace ZingThingFunctions.Services.Interfaces
{
    public interface ITwilioValidatorService
    {
        bool IsValidRequest(HttpRequest request);
    }
}
