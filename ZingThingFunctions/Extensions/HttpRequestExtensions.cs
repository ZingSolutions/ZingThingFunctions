using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Reflection;

namespace ZingThingFunctions.Extensions
{
    public static class HttpRequestExtensions
    {
        public static async System.Threading.Tasks.Task<T> DeserializeFormDataToObjectAsync<T>(this HttpRequest req) where T : class, new()
        {
            var postData = await req.ReadFormAsync();
            T result = new T();
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                if (postData.ContainsKey(prop.Name))
                {
                    prop.SetValue(result, postData[prop.Name].FirstOrDefault());
                }
            }
            return result;
        }
    }
}
