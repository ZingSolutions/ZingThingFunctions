using Newtonsoft.Json;
using System;

namespace ZingThingFunctions.Models
{
    /// <summary>
    /// cosmos wrapper for objects to be stored in the
    /// <see cref="Constants.Cosmos.CollectionNames.Registrations"/> collection
    /// </summary>
    public class RegistrationItem<T> where T : class
    {
        [Obsolete("use only for deserialisation", true)]
        public RegistrationItem() { }

        public RegistrationItem(string id, T item)
        {
            Id = id;
            Type = typeof(T).Name;
            CreatedAtUtc = DateTime.UtcNow;
            Item = item;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [JsonProperty("item")]
        public T Item { get; set; }

        [JsonProperty("_etag")]
        public string ETag { get; set; }
    }
}
