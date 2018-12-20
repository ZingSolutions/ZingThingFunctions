using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZingThingFunctions.Models;
using static ZingThingFunctions.Constants.Cosmos;

namespace ZingThingFunctions.Extensions
{
    public static class DocumentClientExtensions
    {
        private static ConcurrentDictionary<string, Uri> _registrationCollectionUris = new ConcurrentDictionary<string, Uri>();
        private static Uri GetRegistrationCollectionUri(string databaseName)
        {
            return _registrationCollectionUris.GetOrAdd(databaseName, (k) =>
            {
                return UriFactory.CreateDocumentCollectionUri(k, CollectionNames.Registrations);
            });
        }

        /// <summary>
        /// gets a single record of type <see cref="T"/>
        /// from the <see cref="CollectionNames.Registrations"/> collection that match the given predicate.
        /// If more that one result is returned an <see cref="Exception"/> will be thrown.
        /// If no results are found the default value of <see cref="T"/> will be returned.
        /// </summary>
        /// <typeparam name="T">type of the stored record</typeparam>
        /// <param name="client">document client</param>
        /// <param name="databaseName">database name</param>
        /// <param name="predicate">query to filter results by</param>
        /// <returns>the matched record if found, otherwise default value of type <see cref="T"/></returns>
        /// <exception cref="Exception">if more than one result matches the given predicate</exception>
        public static T GetMatchingRegistrationItem<T>(this DocumentClient client, string databaseName, Func<RegistrationItem<T>, bool> predicate) where T : class, new()
        {
            var records = GetMatchingRegistrationItems<T>(client, databaseName, predicate);
            if (records == null) { return null; }

            var count = Enumerable.Count(records);
            if (count > 1) { throw new Exception($"{count} records returned by query, {nameof(GetMatchingRegistrationItem)} expects a single result"); }
            return records.FirstOrDefault();
        }

        /// <summary>
        /// gets registration items of type <see cref="T"/>
        /// from the <see cref="CollectionNames.Registrations"/> collection that match the given predicate.
        /// </summary>
        /// <typeparam name="T">type of the stored record</typeparam>
        /// <param name="client">document client</param>
        /// <param name="databaseName">database name</param>
        /// <param name="predicate">query to filter results by</param>
        /// <returns>IEnumerable of matching results</returns>
        public static IEnumerable<T> GetMatchingRegistrationItems<T>(this DocumentClient client, string databaseName, Func<RegistrationItem<T>, bool> predicate) where T : class, new()
        {
            var items = client.CreateDocumentQuery<RegistrationItem<T>>(
                GetRegistrationCollectionUri(databaseName),
                new FeedOptions() { PartitionKey = new PartitionKey(typeof(T).Name) }
                )
                .Where(predicate)
                .Select(e => e.Item);

            return items ?? new List<T>();
        }
    }
}
