using Microsoft.Extensions.Configuration;
using System.Reflection;
using ZingThingFunctions.Models;
using ZingThingFunctions.Services.Interfaces;

namespace ZingThingFunctions.Services
{
    public class ConfigurationAppSettingsService : IAppSettingsService
    {
        private const string LocalEnvironmentMode = "local";

        public ConfigurationAppSettingsService(IConfiguration configuration)
        {
            AppSettings result = new AppSettings();
            PropertyInfo[] properties = typeof(AppSettings).GetProperties();
            foreach (var prop in properties)
            {
                prop.SetValue(result, configuration.GetValue(prop.PropertyType, prop.Name));
            }
            AppSettings = result;
        }
        public AppSettings AppSettings { get; }
    }
}
